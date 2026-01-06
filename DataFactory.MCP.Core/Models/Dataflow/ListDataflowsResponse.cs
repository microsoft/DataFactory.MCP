using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Dataflow;

/// <summary>
/// Response model for listing dataflows
/// </summary>
public class ListDataflowsResponse
{
    /// <summary>
    /// A list of Dataflows
    /// </summary>
    [JsonPropertyName("value")]
    public List<Dataflow> Value { get; set; } = new();

    /// <summary>
    /// The token for the next result set batch. If there are no more records, it's removed from the response.
    /// </summary>
    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }

    /// <summary>
    /// The URI of the next result set batch. If there are no more records, it's removed from the response.
    /// </summary>
    [JsonPropertyName("continuationUri")]
    public string? ContinuationUri { get; set; }
}