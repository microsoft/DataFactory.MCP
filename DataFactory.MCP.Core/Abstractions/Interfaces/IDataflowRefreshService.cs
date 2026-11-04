using DataFactory.MCP.Models.Dataflow.BackgroundTask;
using ModelContextProtocol;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// High-level service for dataflow refresh operations.
/// This is what tools use - provides a clean API over the background job system.
/// </summary>
public interface IDataflowRefreshService
{
    /// <summary>
    /// Starts a dataflow refresh. Hosts without an MCP session receive the initial
    /// job result without background monitoring.
    /// </summary>
    Task<DataflowRefreshResult> StartRefreshAsync(
        string workspaceId,
        string dataflowId,
        string? displayName = null,
        string executeOption = ExecuteOptions.SkipApplyChanges,
        List<ItemJobParameter>? parameters = null);

    /// <summary>
    /// Starts a dataflow refresh in the background with session notifications.
    /// </summary>
    Task<DataflowRefreshResult> StartRefreshAsync(
        McpSession session,
        string workspaceId,
        string dataflowId,
        string? displayName = null,
        string executeOption = ExecuteOptions.SkipApplyChanges,
        List<ItemJobParameter>? parameters = null);

    /// <summary>
    /// Gets the status of a running refresh.
    /// </summary>
    Task<DataflowRefreshResult> GetStatusAsync(DataflowRefreshContext context);
}
