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
    /// Starts a dataflow refresh in the background.
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
