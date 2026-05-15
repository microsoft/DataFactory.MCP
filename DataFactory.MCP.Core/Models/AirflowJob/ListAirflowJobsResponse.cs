using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.AirflowJob;

/// <summary>
/// Response model for listing Apache Airflow Jobs
/// </summary>
public class ListAirflowJobsResponse
{
    /// <summary>
    /// A list of Apache Airflow Jobs
    /// </summary>
    [JsonPropertyName("value")]
    public List<AirflowJob> Value { get; set; } = new();

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
