namespace DataFactory.MCP.Models.Common.Responses;

/// <summary>
/// Standard success response with data
/// </summary>
public class McpSuccessResponse<T> : McpResponseBase
{
    public McpSuccessResponse(T data, string? message = null)
    {
        Success = true;
        Data = data;
        Message = message;
    }

    public T Data { get; set; }
}