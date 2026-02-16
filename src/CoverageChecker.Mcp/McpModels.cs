namespace CoverageChecker.Mcp;

/// <summary>
/// Represents an MCP request.
/// </summary>
public record McpRequest(string Jsonrpc, object? Id, string Method, object? Params);

/// <summary>
/// Represents an MCP response.
/// </summary>
public record McpResponse(string Jsonrpc, object? Id, object? Result = null, McpError? Error = null);

/// <summary>
/// Represents an MCP error.
/// </summary>
public record McpError(int Code, string Message, object? Data = null);

/// <summary>
/// Represents an MCP tool.
/// </summary>
public record McpTool(string Name, string Description, object InputSchema);

/// <summary>
/// Represents a response containing a list of MCP tools.
/// </summary>
public record McpToolListResponse(IEnumerable<McpTool> Tools);

/// <summary>
/// Represents an MCP call tool request.
/// </summary>
public record McpCallToolRequest(string Name, IDictionary<string, object>? Arguments);

/// <summary>
/// Represents an MCP call tool response.
/// </summary>
public record McpCallToolResponse(IEnumerable<McpContent> Content, bool IsError = false);

/// <summary>
/// Represents MCP content.
/// </summary>
public record McpContent(string Type, string Text);
