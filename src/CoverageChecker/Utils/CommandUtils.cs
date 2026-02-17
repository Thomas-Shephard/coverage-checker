using Microsoft.Extensions.Logging;

namespace CoverageChecker.Utils;

internal static partial class CommandUtils
{
    /// <summary>
    /// Prepares a command for execution by replacing the {output} placeholder with the specified output directory.
    /// </summary>
    /// <param name="commandTemplate">The command template containing the {output} placeholder.</param>
    /// <param name="outputDir">The directory to replace the placeholder with.</param>
    /// <param name="logger">The logger to log the prepared command.</param>
    /// <returns>The prepared command.</returns>
    public static string PrepareCommand(string commandTemplate, string outputDir, ILogger logger)
    {
        string escapedPath = $"\"{outputDir.Replace("\"", "\\\"")}\"";
        string command = commandTemplate.Replace("{output}", escapedPath);
        
        LogRunningCommand(logger, command);
        
        return command;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Prepared command from template by replacing {{output}}: {Command}")]
    private static partial void LogRunningCommand(ILogger logger, string command);
}
