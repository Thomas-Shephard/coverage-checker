using System.Diagnostics;

namespace CoverageChecker.Services;

internal interface IProcessExecutor
{
    (int ExitCode, string StandardOutput, string StandardError) Execute(string fileName, IEnumerable<string> arguments, TimeSpan? timeout = null);
}

internal class ProcessExecutor : IProcessExecutor
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public (int ExitCode, string StandardOutput, string StandardError) Execute(string fileName, IEnumerable<string> arguments, TimeSpan? timeout = null)
    {
        using Process process = new();
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
            catch
            {
                // Ignore cleanup faults
            }

            // Wait a short time for tasks to complete to avoid unobserved task exceptions
            Task.WaitAll([stdoutTask, stderrTask], TimeSpan.FromSeconds(1));

            // Ensure any exceptions from the tasks are observed if they didn't finish within the 1-second wait
            _ = stdoutTask.ContinueWith(task => task.Exception, TaskContinuationOptions.OnlyOnFaulted);
            _ = stderrTask.ContinueWith(task => task.Exception, TaskContinuationOptions.OnlyOnFaulted);

            throw new GitException($"Process '{fileName}' timed out after {(timeout ?? DefaultTimeout).TotalSeconds} seconds.");
        }

        Task.WaitAll(stdoutTask, stderrTask);
        return (process.ExitCode, stdoutTask.Result, stderrTask.Result);
    }
}