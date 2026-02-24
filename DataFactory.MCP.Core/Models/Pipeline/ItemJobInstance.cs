using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Pipeline;

/// <summary>
/// Represents a job instance for a pipeline item in Microsoft Fabric
/// </summary>
public class ItemJobInstance
{
    /// <summary>
    /// Job instance ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Item ID
    /// </summary>
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// Job type (e.g., "Pipeline")
    /// </summary>
    [JsonPropertyName("jobType")]
    public string JobType { get; set; } = string.Empty;

    /// <summary>
    /// How the job was invoked (e.g., "Manual", "Scheduled")
    /// </summary>
    [JsonPropertyName("invokeType")]
    public string InvokeType { get; set; } = string.Empty;

    /// <summary>
    /// Job status (NotStarted, InProgress, Completed, Failed, Cancelled, Deduped)
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Root activity ID for tracing
    /// </summary>
    [JsonPropertyName("rootActivityId")]
    public string? RootActivityId { get; set; }

    /// <summary>
    /// Job start time in UTC
    /// </summary>
    [JsonPropertyName("startTimeUtc")]
    public string? StartTimeUtc { get; set; }

    /// <summary>
    /// Job end time in UTC
    /// </summary>
    [JsonPropertyName("endTimeUtc")]
    public string? EndTimeUtc { get; set; }

    /// <summary>
    /// Error details when job fails
    /// </summary>
    [JsonPropertyName("failureReason")]
    public object? FailureReason { get; set; }
}
