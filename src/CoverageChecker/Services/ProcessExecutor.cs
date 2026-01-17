using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoverageChecker.Services;

internal interface IProcessExecutor
{
    (int ExitCode, string StandardOutput, string StandardError) Execute(string fileName, IEnumerable<string> arguments, string? workingDirectory = null, TimeSpan? timeout = null);
}

internal partial class ProcessExecutor : IProcessExecutor
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private readonly Func<ISystemProcess> _processFactory;
    private readonly string? _workingDirectory;
    private readonly ILogger<ProcessExecutor> _logger;

    public ProcessExecutor(string? workingDirectory = null, ILogger<ProcessExecutor>? logger = null) : this(() => new SystemProcess(), workingDirectory, logger) { }

    internal ProcessExecutor(Func<ISystemProcess> processFactory, string? workingDirectory, ILogger<ProcessExecutor>? logger = null)
    {
        _processFactory = processFactory;
        _workingDirectory = workingDirectory;
        _logger = logger ?? NullLogger<ProcessExecutor>.Instance;
    }

    public (int ExitCode, string StandardOutput, string StandardError) Execute(string fileName, IEnumerable<string> arguments, string? workingDirectory = null, TimeSpan? timeout = null)
    {
        using ISystemProcess process = _processFactory();
        
        string? effectiveWorkingDirectory = workingDirectory ?? _workingDirectory;
        if (!string.IsNullOrEmpty(effectiveWorkingDirectory))
        {
            process.StartInfo.WorkingDirectory = effectiveWorkingDirectory;
        }
        process.StartInfo.FileName = fileName;
        foreach (string argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.Start();

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();

        bool exited = process.WaitForExit((int)(timeout ?? DefaultTimeout).TotalMilliseconds);

        if (!exited)
        {
            try
            {
                process.Kill();
            }
            catch (Exception ex)
            {
                LogProcessKillFailed(ex, fileName);
            }

            // Wait a short time for tasks to complete to avoid unobserved task exceptions
            Task.WaitAll([stdoutTask, stderrTask], TimeSpan.FromSeconds(1));

            // Ensure any exceptions from the tasks are observed if they didn't finish within the 1-second wait
            _ = stdoutTask.ContinueWith(task => task.Exception, TaskContinuationOptions.OnlyOnFaulted);
            _ = stderrTask.ContinueWith(task => task.Exception, TaskContinuationOptions.OnlyOnFaulted);

            int timeoutSeconds = (int)(timeout ?? DefaultTimeout).TotalSeconds;
            throw new ProcessExecutionException($"Process '{fileName}' timed out after {timeoutSeconds} second{(timeoutSeconds == 1 ? string.Empty : "s")}.");
        }

        Task.WaitAll(stdoutTask, stderrTask);
        return (process.ExitCode, stdoutTask.Result, stderrTask.Result);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to kill process '{FileName}' after timeout.")]
    private partial void LogProcessKillFailed(Exception exception, string fileName);
}