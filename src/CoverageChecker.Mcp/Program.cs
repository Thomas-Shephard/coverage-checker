using CoverageChecker.Mcp;
using Microsoft.Extensions.Logging;

// Force UTF-8 for MCP communication
Console.InputEncoding = System.Text.Encoding.UTF8;
Console.OutputEncoding = System.Text.Encoding.UTF8;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddFilter("CoverageChecker", LogLevel.Warning);
    builder.AddFilter("CoverageChecker.Mcp", LogLevel.Warning);
    // We log to Stderr to avoid interfering with MCP Stdio communication on Stdout
    builder.AddConsole(c => c.LogToStandardErrorThreshold = LogLevel.Trace);
});

ILogger logger = loggerFactory.CreateLogger("Program");
Log.ServerStarting(logger);

using McpServer server = new(loggerFactory);
await server.RunAsync(Console.In, Console.Out);
return 0;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Coverage Checker MCP Server starting...")]
    public static partial void ServerStarting(ILogger logger);
}
