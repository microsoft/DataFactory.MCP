using DataFactory.MCP.Models.Common.Responses.Errors;

namespace DataFactory.MCP.Handlers;

/// <summary>
/// Extension methods for converting ToolResult failures to MCP error responses.
/// </summary>
public static class ToolResultExtensions
{
    /// <summary>
    /// Converts a failed ToolResult to the appropriate MCP error response object.
    /// </summary>
    public static object ToErrorResponse<T>(this ToolResult<T> result, string operation = "executing tool")
    {
        return result.ErrorType switch
        {
            "validation" => new McpValidationErrorResponse(result.Error ?? "Validation failed"),
            "auth" => new McpAuthenticationErrorResponse(result.Error ?? "Authentication failed"),
            "http" => new McpHttpErrorResponse(result.Error ?? "HTTP request failed"),
            _ => new McpOperationErrorResponse(result.Error ?? "Unknown error", operation),
        };
    }
}
