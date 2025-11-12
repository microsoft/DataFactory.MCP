namespace DataFactory.MCP.Models.Common.Responses.Errors;

/// <summary>
/// Operation error response for general operation failures
/// </summary>
public class McpOperationErrorResponse : McpErrorResponse
{
    public McpOperationErrorResponse(string message, string operation)
        : base("OperationError", $"Error {operation}: {message}")
    {
        Operation = operation;
    }

    public string Operation { get; set; }
}