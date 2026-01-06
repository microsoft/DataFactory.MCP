using DataFactory.MCP.Models.Workspace;

namespace DataFactory.MCP.Extensions;

/// <summary>
/// Extension methods for Workspace model transformations.
/// </summary>
public static class WorkspaceExtensions
{
    /// <summary>
    /// Formats a Workspace object for MCP API responses.
    /// Provides consistent output format and handles optional properties appropriately.
    /// </summary>
    /// <param name="workspace">The workspace object to format</param>
    /// <returns>Formatted object ready for JSON serialization</returns>
    public static object ToFormattedInfo(this Workspace workspace)
    {
        var formattedInfo = new
        {
            Id = workspace.Id,
            DisplayName = workspace.DisplayName,
            Description = workspace.Description,
            Type = workspace.Type.ToString(),
            CapacityId = workspace.CapacityId,
            DomainId = workspace.DomainId,
            ApiEndpoint = workspace.ApiEndpoint
        };

        return formattedInfo;
    }
}