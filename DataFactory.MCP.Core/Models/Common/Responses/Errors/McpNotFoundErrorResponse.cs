namespace DataFactory.MCP.Models.Common.Responses.Errors;

/// <summary>
/// Not found error response for resource not found scenarios
/// </summary>
public class McpNotFoundErrorResponse : McpErrorResponse
{
    public McpNotFoundErrorResponse(string resourceType, string resourceId)
        : base("NotFoundError", $"{resourceType} with ID '{resourceId}' was not found")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public string ResourceType { get; set; }
    public string ResourceId { get; set; }
}