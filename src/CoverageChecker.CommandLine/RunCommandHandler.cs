using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace CoverageChecker.CommandLine;

internal static partial class RunCommandHandler
{
    public static async Task<int> Run(
        RunOptions options,
        Func<CommandLineOptions, bool?, ILoggerFactory?, Task<int>> runCoverageCheck,
        Func<bool, ILoggerFactory> createLoggerFactory)
    {
        bool isGitHubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";
        using ILoggerFactory loggerFactory = createLoggerFactory(isGitHubActions);
        ILogger logger = loggerFactory.CreateLogger("CoverageChecker.CommandLine");
        string? tempDir = null;

        try
        {
            string workingDirectory = Path.GetFullPath(options.WorkingDirectory ?? Environment.CurrentDirectory);
            string outputDir = Path.GetFullPath(options.Output ?? Path.Combine(Path.GetTempPath(), "coverage-checker", Guid.NewGuid().ToString()));
            tempDir = options.Output == null ? outputDir : null;

            if (!Directory.Exists(workingDirectory))
            {
                logger.LogWorkingDirectoryNotFound(workingDirectory);
                return 1;
            }

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string command = PrepareCommand(options.Command, outputDir, logger);

            int exitCode = await ExecuteCommand(command, workingDirectory, options.Timeout, logger);

            if (exitCode != 0)
            {
                if (options.ContinueOnFailure)
                {
                    logger.LogCommandFailedWarning(exitCode);
                }
                else
                {
                    logger.LogCommandFailed(exitCode);
                    return exitCode;
                }
            }

            CommandLineOptions effectiveOptions = options with { Directory = outputDir };
            return await runCoverageCheck(effectiveOptions, isGitHubActions, loggerFactory);
        }
        catch (Exception ex)
        {
            logger.LogCriticalError(ex);
            return 1;
        }
        finally
        {
            CleanupTempDirectory(tempDir, logger);
        }
    }

    private static string PrepareCommand(string commandTemplate, string outputDir, ILogger logger)
    {
        string command = OutputPathSuffixRegex().Replace(
            commandTemplate,
            match => QuoteShellPath(outputDir + match.Groups[1].Value));

        command = command.Replace("{output}", QuoteShellPath(outputDir));
        logger.LogRunningCommand(command);
        return command;
    }

    private static string QuoteShellPath(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string normalizedPath = path.StartsWith(@"\\", StringComparison.Ordinal)
                ? @"\\" + path[2..].Replace('\\', '/')
                : path.Replace('\\', '/');

            return $"\"{normalizedPath.Replace("\"", "\"\"")}\"";
        }

        return $"'{path.Replace("'", "'\\''")}'";
    }

    private static async Task<int> ExecuteCommand(string command, string workingDirectory, int timeoutMinutes, ILogger logger)
    {
        using Process process = new();
        process.StartInfo.FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "sh";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            process.StartInfo.Arguments = $"/d /s /c \"{command}\"";
        }
        else
        {
            process.StartInfo.ArgumentList.Add("-c");
            process.StartInfo.ArgumentList.Add(command);
        }

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.WorkingDirectory = workingDirectory;
        // Do not redirect to allow inheriting the parent console's stdout/stderr (real-time output)
        process.StartInfo.RedirectStandardOutput = false;
        process.StartInfo.RedirectStandardError = false;

        process.Start();

        TimeSpan timeout = timeoutMinutes == -1
            ? Timeout.InfiniteTimeSpan
            : TimeSpan.FromMinutes(timeoutMinutes);
        using CancellationTokenSource cts = new(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            logger.LogCommandTimedOut(timeoutMinutes);
            return 1;
        }

        return process.ExitCode;
    }

    private static void CleanupTempDirectory(string? tempDir, ILogger logger)
    {
        if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                logger.LogCleanupFailed(ex, tempDir);
            }
        }
    }

    [GeneratedRegex(@"\{output\}([\\/][^\s&|;<>""']*)")]
    private static partial Regex OutputPathSuffixRegex();
}
