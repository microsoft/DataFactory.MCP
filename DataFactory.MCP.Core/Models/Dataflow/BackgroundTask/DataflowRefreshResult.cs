namespace DataFactory.MCP.Models.Dataflow.BackgroundTask;

/// <summary>
/// Result of a dataflow refresh operation (either immediate or polled status)
/// </summary>
public record DataflowRefreshResult
{
    /// <summary>
    /// Whether the refresh operation has completed (success, failed, or cancelled)
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// Current status: NotStarted, InProgress, Completed, Failed, Cancelled, Deduped
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Context for tracking this refresh (for polling)
    /// </summary>
    public DataflowRefreshContext? Context { get; init; }

    /// <summary>
    /// When the job finished (if complete)
    /// </summary>
    public DateTime? EndTimeUtc { get; init; }

    /// <summary>
    /// Error message if the refresh failed
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// Calculated duration of the refresh
    /// </summary>
    public TimeSpan? Duration => EndTimeUtc.HasValue && Context != null
        ? EndTimeUtc.Value - Context.StartedAtUtc
        : null;

    /// <summary>
    /// Human-readable duration string
    /// </summary>
    public string? DurationFormatted => Duration?.ToString(@"hh\:mm\:ss");

    /// <summary>
    /// Error message if operation failed to start
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Whether this is a successful completion
    /// </summary>
    public bool IsSuccess => IsComplete && Status == "Completed";
}
