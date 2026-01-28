namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Represents a background job that can be executed and monitored.
/// Each job type (dataflow refresh, pipeline run, etc.) implements this interface.
/// </summary>
public interface IBackgroundJob
{
    /// <summary>
    /// Unique identifier for this job instance.
    /// </summary>
    string JobId { get; }

    /// <summary>
    /// Type of job (e.g., "DataflowRefresh", "PipelineRun").
    /// </summary>
    string JobType { get; }

    /// <summary>
    /// User-friendly display name for notifications.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Starts the job execution (e.g., calls API to trigger refresh).
    /// </summary>
    Task<BackgroundJobResult> StartAsync();

    /// <summary>
    /// Checks the current status of the job.
    /// </summary>
    Task<BackgroundJobResult> CheckStatusAsync();
}

/// <summary>
/// Result of a background job operation.
/// </summary>
public record BackgroundJobResult
{
    /// <summary>
    /// Whether the job has completed (success, failed, or cancelled).
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// Whether the job completed successfully.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Current status string (e.g., "InProgress", "Completed", "Failed").
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Error message if the job failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// When the job started.
    /// </summary>
    public DateTime StartedAt { get; init; }

    /// <summary>
    /// When the job completed (if complete).
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Human-readable duration string.
    /// </summary>
    public string? DurationFormatted { get; init; }

    /// <summary>
    /// Additional context data specific to the job type.
    /// </summary>
    public object? Context { get; init; }
}
