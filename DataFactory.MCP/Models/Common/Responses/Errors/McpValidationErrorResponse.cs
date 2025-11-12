namespace DataFactory.MCP.Models.Common.Responses.Errors;

/// <summary>
/// Validation error response for parameter and input validation failures
/// </summary>
public class McpValidationErrorResponse : McpErrorResponse
{
    public McpValidationErrorResponse(string message)
        : base("ValidationError", $"Validation failed: {message}")
    {
    }
}