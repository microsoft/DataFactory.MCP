using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Capacity;

/// <summary>
/// Response for listing capacities
/// </summary>
public class ListCapacitiesResponse
{
    /// <summary>
    /// A list of capacities returned
    /// </summary>
    [JsonPropertyName("value")]
    public List<Capacity> Value { get; set; } = new();

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