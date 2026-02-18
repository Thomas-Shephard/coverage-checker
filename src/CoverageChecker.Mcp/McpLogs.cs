using Microsoft.Extensions.Logging;

namespace CoverageChecker.Mcp;

internal static partial class McpLogs
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Coverage Checker MCP Server starting...")]
    public static partial void LogServerStarting(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Received: {Line}")]
    public static partial void LogReceivedLine(this ILogger logger, string line);

    [LoggerMessage(Level = LogLevel.Information, Message = "Client initialized.")]
    public static partial void LogClientInitialized(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing MCP request")]
    public static partial void LogMcpRequestProcessingError(this ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to cleanup coverage reports matching {ReportPath}")]
    public static partial void LogCleanupError(this ILogger logger, Exception ex, string reportPath);
}
