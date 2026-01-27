using DataFactory.MCP.Models.Dataflow.BackgroundTask;
using ModelContextProtocol;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Service for managing background tasks with MCP notification support
/// </summary>
public interface IBackgroundTaskManager
{
    /// <summary>
    /// Starts a dataflow refresh and monitors it in the background.
    /// Sends an MCP notification when complete.
    /// </summary>
    /// <param name="session">The MCP session to send notifications through</param>
    /// <param name="workspaceId">Workspace ID containing the dataflow</param>
    /// <param name="dataflowId">Dataflow ID to refresh</param>
    /// <param name="displayName">User-friendly name for notifications</param>
    /// <param name="executeOption">Execute option (SkipApplyChanges or ApplyChangesIfNeeded)</param>
    /// <param name="parameters">Optional parameter overrides</param>
    /// <returns>Initial result with task context</returns>
    Task<DataflowRefreshResult> StartDataflowRefreshAsync(
        McpSession session,
        string workspaceId,
        string dataflowId,
        string? displayName = null,
        string executeOption = ExecuteOptions.SkipApplyChanges,
        List<ItemJobParameter>? parameters = null);

    /// <summary>
    /// Gets the current status of a refresh operation (for manual polling)
    /// </summary>
    Task<DataflowRefreshResult> GetRefreshStatusAsync(DataflowRefreshContext context);

    /// <summary>
    /// Gets all currently tracked background tasks
    /// </summary>
    IReadOnlyList<BackgroundTaskInfo> GetAllTasks();

    /// <summary>
    /// Gets info about a specific task
    /// </summary>
    BackgroundTaskInfo? GetTask(string taskId);
}

/// <summary>
/// Information about a tracked background task
/// </summary>
public record BackgroundTaskInfo
{
    public required string TaskId { get; init; }
    public required string TaskType { get; init; }
    public required string DisplayName { get; init; }
    public required string Status { get; init; }
    public required DateTime StartedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public string? FailureReason { get; init; }
    public DataflowRefreshContext? Context { get; init; }
}
