using DataFactory.MCP.Models.Workspace;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Service for interacting with Microsoft Fabric Workspaces API
/// </summary>
public interface IFabricWorkspaceService
{
    /// <summary>
    /// Lists all workspaces the user has permission for
    /// </summary>
    /// <param name="roles">A list of roles. Separate values using a comma. If not provided, all workspaces are returned.</param>
    /// <param name="continuationToken">A token for retrieving the next page of results</param>
    /// <param name="preferWorkspaceSpecificEndpoints">A setting that controls whether to include the workspace-specific API endpoint per workspace</param>
    /// <returns>List of workspaces</returns>
    Task<ListWorkspacesResponse> ListWorkspacesAsync(
        string? roles = null,
        string? continuationToken = null,
        bool? preferWorkspaceSpecificEndpoints = null);
}