using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.CopyJob;

/// <summary>
/// Response model for listing copy jobs
/// </summary>
public class ListCopyJobsResponse
{
    /// <summary>
    /// A list of Copy Jobs
    /// </summary>
    [JsonPropertyName("value")]
    public List<CopyJob> Value { get; set; } = new();

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
