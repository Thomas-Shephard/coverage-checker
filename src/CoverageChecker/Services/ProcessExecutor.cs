using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace CoverageChecker.Services;

internal interface IProcessExecutor
{
    (int ExitCode, string StandardOutput, string StandardError) Execute(string fileName, IEnumerable<string> arguments, TimeSpan? timeout = null);
    (int ExitCode, string StandardOutput, string StandardError) Execute(string fileName, IEnumerable<string> arguments, string? workingDirectory, TimeSpan? timeout = null);
    (int ExitCode, string StandardOutput, string StandardError) ExecuteShell(string command, string? workingDirectory = null, TimeSpan? timeout = null);
    Task<(int ExitCode, string StandardOutput, string StandardError)> ExecuteAsync(string fileName, IEnumerable<string> arguments, string? workingDirectory = null, TimeSpan? timeout = null);
    Task<(int ExitCode, string StandardOutput, string StandardError)> ExecuteShellAsync(string command, string? workingDirectory = null, TimeSpan? timeout = null);
}

internal partial class ProcessExecutor : IProcessExecutor
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private readonly Func<ISystemProcess> _processFactory;
    private readonly string? _workingDirectory;
    private readonly ILogger<ProcessExecutor> _logger;

    public bool RedirectOutput { get; init; } = true;

    public ProcessExecutor(string? workingDirectory = null, ILogger<ProcessExecutor>? logger = null) : this(() => new SystemProcess(), workingDirectory, logger) { }

    internal ProcessExecutor(Func<ISystemProcess> processFactory, string? workingDirectory, ILogger<ProcessExecutor>? logger = null)
    {
        _processFactory = processFactory;
        _workingDirectory = workingDirectory;
        _logger = logger ?? NullLogger<ProcessExecutor>.Instance;
    }

    public (int ExitCode, string StandardOutput, string StandardError) Execute(string fileName, IEnumerable<string> arguments, TimeSpan? timeout = null)
        => Execute(fileName, arguments, null, timeout);

    public (int ExitCode, string StandardOutput, string StandardError) Execute(string fileName, IEnumerable<string> arguments, string? workingDirectory, TimeSpan? timeout = null)
    {
        using ISystemProcess process = _processFactory();
        
        ConfigureStartInfo(process.StartInfo, fileName, arguments, workingDirectory);

        return InternalExecute(process, fileName, timeout);
    }

    public (int ExitCode, string StandardOutput, string StandardError) ExecuteShell(string command, string? workingDirectory = null, TimeSpan? timeout = null)
    {
        using ISystemProcess process = _processFactory();

        ConfigureShellStartInfo(process.StartInfo, command, workingDirectory);

        return InternalExecute(process, command, timeout);
    }

    public async Task<(int ExitCode, string StandardOutput, string StandardError)> ExecuteAsync(string fileName, IEnumerable<string> arguments, string? workingDirectory = null, TimeSpan? timeout = null)
    {
        using ISystemProcess process = _processFactory();
        
        ConfigureStartInfo(process.StartInfo, fileName, arguments, workingDirectory);

        return await InternalExecuteAsync(process, fileName, timeout);
    }

    public async Task<(int ExitCode, string StandardOutput, string StandardError)> ExecuteShellAsync(string command, string? workingDirectory = null, TimeSpan? timeout = null)
    {
        using ISystemProcess process = _processFactory();

        ConfigureShellStartInfo(process.StartInfo, command, workingDirectory);

        return await InternalExecuteAsync(process, command, timeout);
    }

    private void ConfigureStartInfo(ProcessStartInfo startInfo, string fileName, IEnumerable<string> arguments, string? workingDirectory)
    {
        string? effectiveWorkingDirectory = workingDirectory ?? _workingDirectory;
        if (!string.IsNullOrEmpty(effectiveWorkingDirectory))
        {
            startInfo.WorkingDirectory = effectiveWorkingDirectory;
        }
        startInfo.FileName = fileName;
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
        startInfo.RedirectStandardOutput = RedirectOutput;
        startInfo.RedirectStandardError = RedirectOutput;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
    }

    private void ConfigureShellStartInfo(ProcessStartInfo startInfo, string command, string? workingDirectory)
    {
        string? effectiveWorkingDirectory = workingDirectory ?? _workingDirectory;
        if (!string.IsNullOrEmpty(effectiveWorkingDirectory))
        {
            startInfo.WorkingDirectory = effectiveWorkingDirectory;
        }

        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        startInfo.FileName = isWindows ? "cmd.exe" : "sh";

        if (isWindows)
        {
            // Use /s and wrap the command in quotes to ensure cmd.exe 
            // preserves the internal quoting of the command string.
            startInfo.Arguments = $"/s /c \"{command}\"";
        }
        else
        {
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add(command);
        }

        startInfo.RedirectStandardOutput = RedirectOutput;
        startInfo.RedirectStandardError = RedirectOutput;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
    }

    private (int ExitCode, string StandardOutput, string StandardError) InternalExecute(ISystemProcess process, string name, TimeSpan? timeout)
    {
        process.Start();

        Task<string> stdoutTask = RedirectOutput ? process.StandardOutput.ReadToEndAsync() : Task.FromResult(string.Empty);
        Task<string> stderrTask = RedirectOutput ? process.StandardError.ReadToEndAsync() : Task.FromResult(string.Empty);

        bool exited = process.WaitForExit((int)(timeout ?? DefaultTimeout).TotalMilliseconds);

        if (!exited)
        {
            try
            {
                process.Kill();
            }
            catch (Exception ex)
            {
                LogProcessKillFailed(ex, name);
            }

            // Wait a short time for tasks to complete to avoid unobserved task exceptions
            Task.WaitAll([stdoutTask, stderrTask], TimeSpan.FromSeconds(1));

            // Ensure any exceptions from the tasks are observed if they didn't finish within the 1-second wait
            _ = stdoutTask.ContinueWith(task => task.Exception, TaskContinuationOptions.OnlyOnFaulted);
            _ = stderrTask.ContinueWith(task => task.Exception, TaskContinuationOptions.OnlyOnFaulted);

            int timeoutSeconds = (int)(timeout ?? DefaultTimeout).TotalSeconds;
            throw new ProcessExecutionException($"Process '{name}' timed out after {timeoutSeconds} second{(timeoutSeconds == 1 ? string.Empty : "s")}.");
        }

        Task.WaitAll(stdoutTask, stderrTask);
        return (process.ExitCode, stdoutTask.Result, stderrTask.Result);
    }

    private async Task<(int ExitCode, string StandardOutput, string StandardError)> InternalExecuteAsync(ISystemProcess process, string name, TimeSpan? timeout)
    {
        process.Start();

        Task<string> stdoutTask = RedirectOutput ? process.StandardOutput.ReadToEndAsync() : Task.FromResult(string.Empty);
        Task<string> stderrTask = RedirectOutput ? process.StandardError.ReadToEndAsync() : Task.FromResult(string.Empty);

        TimeSpan effectiveTimeout = timeout ?? DefaultTimeout;
        using CancellationTokenSource cts = new(effectiveTimeout);

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill();
            }
            catch (Exception ex)
            {
                LogProcessKillFailed(ex, name);
            }

            // Wait a short time for tasks to complete to avoid unobserved task exceptions
            await Task.WhenAny(Task.WhenAll(stdoutTask, stderrTask), Task.Delay(TimeSpan.FromSeconds(1)));

            int timeoutSeconds = (int)effectiveTimeout.TotalSeconds;
            throw new ProcessExecutionException($"Process '{name}' timed out after {timeoutSeconds} second{(timeoutSeconds == 1 ? string.Empty : "s")}.");
        }

        await Task.WhenAll(stdoutTask, stderrTask);
        return (process.ExitCode, stdoutTask.Result, stderrTask.Result);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to kill process '{FileName}' after timeout.")]
    private partial void LogProcessKillFailed(Exception exception, string fileName);
}
