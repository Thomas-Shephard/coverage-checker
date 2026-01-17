using CoverageChecker.Mcp;
using Microsoft.Extensions.Logging;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddFilter("CoverageChecker", LogLevel.Warning);
    builder.AddFilter("CoverageChecker.Mcp", LogLevel.Warning);
    // We log to Stderr to avoid interfering with MCP Stdio communication on Stdout
    builder.AddConsole(c => c.LogToStandardErrorThreshold = LogLevel.Trace);
});

var server = new McpServer(loggerFactory);
await server.RunAsync(Console.In, Console.Out);
return 0;
