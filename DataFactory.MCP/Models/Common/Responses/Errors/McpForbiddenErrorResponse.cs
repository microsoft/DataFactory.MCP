namespace DataFactory.MCP.Models.Common.Responses.Errors;

/// <summary>
/// Forbidden access error response for permission and access control failures
/// </summary>
public class McpForbiddenErrorResponse : McpErrorResponse
{
    public McpForbiddenErrorResponse(string message)
        : base("ForbiddenError", $"Access denied: {message}")
    {
    }
}