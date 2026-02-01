using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace CoverageChecker.CommandLine;

internal sealed class GitHubWorkflowFormatter() : ConsoleFormatter("github")
{
    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        string message = logEntry.Formatter(logEntry.State, logEntry.Exception);

        if (logEntry.Exception is not null)
        {
            message += Environment.NewLine + logEntry.Exception;
        }

        if (string.IsNullOrEmpty(message)) return;

        string? command = logEntry.LogLevel switch
        {
            LogLevel.Error or LogLevel.Critical => "error",
            LogLevel.Warning                    => "warning",
            LogLevel.Information                => logEntry.Category.StartsWith("CoverageChecker.CommandLine", StringComparison.Ordinal) ? "notice" : "debug",
            LogLevel.Debug                      => "debug",
            _                                   => null
        };

        if (command is not null)
        {
            message = message.Replace("%", "%25").Replace("\n", "%0A").Replace("\r", "%0D");
            textWriter.WriteLine($"::{command}::{message}");
        }
        else
        {
            textWriter.WriteLine(message);
        }
    }
}
