using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Pipeline.Schedule;

/// <summary>
/// Response containing a list of schedules for a pipeline item
/// </summary>
public class ListSchedulesResponse
{
    /// <summary>
    /// List of schedules
    /// </summary>
    [JsonPropertyName("value")]
    public List<ItemSchedule> Value { get; set; } = new();

    /// <summary>
    /// Continuation token for pagination
    /// </summary>
    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }

    /// <summary>
    /// Continuation URI for pagination
    /// </summary>
    [JsonPropertyName("continuationUri")]
    public string? ContinuationUri { get; set; }
}
