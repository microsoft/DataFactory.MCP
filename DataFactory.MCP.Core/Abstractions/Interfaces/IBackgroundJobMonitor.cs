using ModelContextProtocol;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Manages the complete lifecycle of background jobs: start, track, monitor, and notify.
/// Consolidates job running and task tracking into a single efficient service.
/// </summary>
public interface IBackgroundJobMonitor
{
    /// <summary>
    /// Starts a job and monitors it until completion.
    /// The monitor will periodically check the job's status and send notifications when done.
    /// </summary>
    /// <param name="job">The job to start and monitor</param>
    /// <param name="session">MCP session for notification context</param>
    /// <returns>The initial job result after starting</returns>
    Task<BackgroundJobResult> StartJobAsync(IBackgroundJob job, McpSession session);

    /// <summary>
    /// Gets a tracked task by ID (active or completed).
    /// </summary>
    TrackedTask? GetTask(string taskId);

    /// <summary>
    /// Gets all tracked tasks.
    /// </summary>
    IReadOnlyList<TrackedTask> GetAllTasks();

    /// <summary>
    /// Gets whether there are any active jobs being monitored.
    /// </summary>
    bool HasActiveJobs { get; }

    /// <summary>
    /// Gets the count of active jobs being monitored.
    /// </summary>
    int ActiveJobCount { get; }
}

/// <summary>
/// Information about a tracked background task.
/// </summary>
public class TrackedTask
{
    public required string TaskId { get; init; }
    public required string JobType { get; init; }
    public required string DisplayName { get; init; }
    public string Status { get; set; } = "Pending";
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
    public object? Context { get; init; }
}
