namespace DataFactory.MCP.Models.Common.Responses;

/// <summary>
/// Base class for all MCP tool responses
/// </summary>
public abstract class McpResponseBase
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}