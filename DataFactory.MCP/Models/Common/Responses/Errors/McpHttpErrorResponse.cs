namespace DataFactory.MCP.Models.Common.Responses.Errors;

/// <summary>
/// HTTP error response for network and HTTP-related failures
/// </summary>
public class McpHttpErrorResponse : McpErrorResponse
{
    public McpHttpErrorResponse(string message)
        : base("HttpRequestError", message)
    {
    }
}