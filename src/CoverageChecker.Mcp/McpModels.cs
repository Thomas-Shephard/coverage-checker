namespace CoverageChecker.Mcp;

/// <summary>
/// Represents an MCP request.
/// </summary>
/// <param name="Jsonrpc">The JSON-RPC version.</param>
/// <param name="Id">The request ID.</param>
/// <param name="Method">The method name.</param>
/// <param name="Params">The method parameters.</param>
public record McpRequest(string Jsonrpc, object? Id, string Method, object? Params);

/// <summary>
/// Represents an MCP response.
/// </summary>
/// <param name="Jsonrpc">The JSON-RPC version.</param>
/// <param name="Id">The request ID.</param>
/// <param name="Result">The response result.</param>
/// <param name="Error">The response error.</param>
public record McpResponse(string Jsonrpc, object? Id, object? Result = null, McpError? Error = null);

/// <summary>
/// Represents an MCP error.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Message">The error message.</param>
/// <param name="Data">Optional error data.</param>
public record McpError(int Code, string Message, object? Data = null);

/// <summary>
/// Represents an MCP tool.
/// </summary>
/// <param name="Name">The tool name.</param>
/// <param name="Description">The tool description.</param>
/// <param name="InputSchema">The JSON schema for tool inputs.</param>
public record McpTool(string Name, string Description, object InputSchema);

/// <summary>
/// Represents a response containing a list of MCP tools.
/// </summary>
/// <param name="Tools">The list of tools.</param>
public record McpToolListResponse(IEnumerable<McpTool> Tools);

/// <summary>
/// Represents an MCP call tool request.
/// </summary>
/// <param name="Name">The tool name.</param>
/// <param name="Arguments">The tool arguments.</param>
public record McpCallToolRequest(string Name, IDictionary<string, object?>? Arguments);

/// <summary>
/// Represents an MCP call tool response.
/// </summary>
/// <param name="Content">The response content.</param>
/// <param name="IsError">Whether the call was an error.</param>
public record McpCallToolResponse(IEnumerable<McpContent> Content, bool IsError = false);

/// <summary>
/// Represents MCP content.
/// </summary>
/// <param name="Type">The content type.</param>
/// <param name="Text">The text content.</param>
public record McpContent(string Type, string Text);
