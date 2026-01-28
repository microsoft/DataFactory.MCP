using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Dataflow.BackgroundTask;

/// <summary>
/// Response from the Fabric API when getting job instance status
/// GET /workspaces/{workspaceId}/items/{itemId}/jobs/instances/{jobInstanceId}
/// </summary>
public class ItemJobInstance
{
    /// <summary>
    /// Job instance ID
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Item ID (dataflow ID)
    /// </summary>
    [JsonPropertyName("itemId")]
    public string? ItemId { get; set; }

    /// <summary>
    /// Job type (e.g., "Execute")
    /// </summary>
    [JsonPropertyName("jobType")]
    public string? JobType { get; set; }

    /// <summary>
    /// How the job was invoked: "Manual" or "Scheduled"
    /// </summary>
    [JsonPropertyName("invokeType")]
    public string? InvokeType { get; set; }

    /// <summary>
    /// Current status: NotStarted, InProgress, Completed, Failed, Cancelled, Deduped
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Root activity ID for tracing
    /// </summary>
    [JsonPropertyName("rootActivityId")]
    public string? RootActivityId { get; set; }

    /// <summary>
    /// When the job started
    /// </summary>
    [JsonPropertyName("startTimeUtc")]
    public DateTime? StartTimeUtc { get; set; }

    /// <summary>
    /// When the job ended
    /// </summary>
    [JsonPropertyName("endTimeUtc")]
    public DateTime? EndTimeUtc { get; set; }

    /// <summary>
    /// Error details if the job failed
    /// </summary>
    [JsonPropertyName("failureReason")]
    public JobFailureReason? FailureReason { get; set; }
}

/// <summary>
/// Error details for a failed job
/// </summary>
public class JobFailureReason
{
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
