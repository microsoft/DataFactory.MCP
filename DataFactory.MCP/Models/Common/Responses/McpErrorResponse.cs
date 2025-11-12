namespace DataFactory.MCP.Models.Common.Responses;

/// <summary>
/// Standard error response base class
/// </summary>
public class McpErrorResponse : McpResponseBase
{
    public McpErrorResponse(string error, string message)
    {
        Success = false;
        Error = error;
        Message = message;
    }

    public string Error { get; set; }
}