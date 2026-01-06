using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Connection;

/// <summary>
/// Response model for listing connections
/// </summary>
public class ListConnectionsResponse
{
    [JsonPropertyName("value")]
    public List<Connection> Value { get; set; } = new();

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }

    [JsonPropertyName("continuationUri")]
    public string? ContinuationUri { get; set; }
}