namespace DataFactory.MCP.Models.Dataflow.BackgroundTask;

/// <summary>
/// Context information for tracking a dataflow refresh operation
/// </summary>
public record DataflowRefreshContext
{
    /// <summary>
    /// The workspace ID containing the dataflow
    /// </summary>
    public required string WorkspaceId { get; init; }

    /// <summary>
    /// The dataflow ID being refreshed
    /// </summary>
    public required string DataflowId { get; init; }

    /// <summary>
    /// The job instance ID returned by the Fabric API
    /// </summary>
    public required string JobInstanceId { get; init; }

    /// <summary>
    /// The location URL for polling job status
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// Suggested seconds to wait before polling
    /// </summary>
    public int RetryAfterSeconds { get; init; } = 60;

    /// <summary>
    /// When the refresh was started
    /// </summary>
    public DateTime StartedAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// User-friendly name for display in notifications
    /// </summary>
    public string? DisplayName { get; init; }
}
