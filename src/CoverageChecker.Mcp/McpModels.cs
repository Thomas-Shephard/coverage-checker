using System.Text.Json.Serialization;

namespace CoverageChecker.Mcp;

public record McpRequest(
    [property: JsonPropertyName("jsonrpc")] string JsonRpc,
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("params")] object? Params,
    [property: JsonPropertyName("id")] object? Id);

public record McpResponse(
    [property: JsonPropertyName("jsonrpc")] string JsonRpc,
    [property: JsonPropertyName("id")] object? Id,
    [property: JsonPropertyName("result")] object? Result = null,
    [property: JsonPropertyName("error")] McpError? Error = null);

public record McpError(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("data")] object? Data = null);

public record McpTool(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("inputSchema")] object InputSchema);

public record McpToolListResponse(
    [property: JsonPropertyName("tools")] IEnumerable<McpTool> Tools);

public record McpCallToolRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("arguments")] IDictionary<string, object>? Arguments);

public record McpCallToolResponse(
    [property: JsonPropertyName("content")] IEnumerable<McpContent> Content,
    [property: JsonPropertyName("isError")] bool IsError = false);

public record McpContent(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string Text);
