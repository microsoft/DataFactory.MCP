namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Tracks running background tasks.
/// Single Responsibility: only manages task state, doesn't execute or monitor.
/// </summary>
public interface IBackgroundTaskTracker
{
    /// <summary>
    /// Adds a task to be tracked.
    /// </summary>
    void Track(TrackedTask task);

    /// <summary>
    /// Updates a tracked task's status.
    /// </summary>
    void Update(string taskId, Action<TrackedTask> updateAction);

    /// <summary>
    /// Gets a specific task by ID.
    /// </summary>
    TrackedTask? GetTask(string taskId);

    /// <summary>
    /// Gets all tracked tasks.
    /// </summary>
    IReadOnlyList<TrackedTask> GetAllTasks();
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
