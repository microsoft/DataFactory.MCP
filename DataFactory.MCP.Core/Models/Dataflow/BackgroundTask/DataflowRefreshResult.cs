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
    /// Human-readable duration string (e.g., "28 seconds", "2 minutes 15 seconds")
    /// </summary>
    public string? DurationFormatted => FormatDuration(Duration);

    private static string? FormatDuration(TimeSpan? duration)
    {
        if (duration == null) return null;

        var ts = duration.Value;

        if (ts.TotalSeconds < 60)
            return ts.Seconds == 1 ? "1 second" : $"{ts.Seconds} seconds";

        if (ts.TotalMinutes < 60)
        {
            var mins = (int)ts.TotalMinutes;
            var secs = ts.Seconds;
            var minPart = mins == 1 ? "1 minute" : $"{mins} minutes";
            if (secs == 0) return minPart;
            var secPart = secs == 1 ? "1 second" : $"{secs} seconds";
            return $"{minPart} {secPart}";
        }

        var hours = (int)ts.TotalHours;
        var minutes = ts.Minutes;
        var hourPart = hours == 1 ? "1 hour" : $"{hours} hours";
        if (minutes == 0) return hourPart;
        var minutePart = minutes == 1 ? "1 minute" : $"{minutes} minutes";
        return $"{hourPart} {minutePart}";
    }

    /// <summary>
    /// Error message if operation failed to start
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Whether this is a successful completion
    /// </summary>
    public bool IsSuccess => IsComplete && Status == "Completed";
}
