namespace DataFactory.MCP.Models.Common.Responses.Errors;

/// <summary>
/// Authentication error response for authorization and authentication failures
/// </summary>
public class McpAuthenticationErrorResponse : McpErrorResponse
{
    public McpAuthenticationErrorResponse(string message)
        : base("AuthenticationError", message)
    {
    }
}