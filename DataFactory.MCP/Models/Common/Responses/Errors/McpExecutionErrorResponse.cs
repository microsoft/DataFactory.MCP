namespace DataFactory.MCP.Models.Common.Responses.Errors;

/// <summary>
/// Generic execution error response for dataflow query and execution failures
/// </summary>
public class McpExecutionErrorResponse : McpErrorResponse
{
    public McpExecutionErrorResponse(string message)
        : base("ExecutionError", $"Error executing dataflow query: {message}")
    {
    }
}