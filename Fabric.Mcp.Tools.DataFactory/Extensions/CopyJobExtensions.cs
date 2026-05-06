using Fabric.Mcp.Tools.DataFactory.Models.CopyJob;

namespace Fabric.Mcp.Tools.DataFactory.Extensions;

/// <summary>
/// Extension methods for CopyJob model transformations.
/// </summary>
public static class CopyJobExtensions
{
    /// <summary>
    /// Formats a CopyJob object for MCP API responses.
    /// </summary>
    public static object ToFormattedInfo(this CopyJob copyJob)
    {
        return new
        {
            Id = copyJob.Id,
            DisplayName = copyJob.DisplayName,
            Description = copyJob.Description,
            Type = copyJob.Type,
            WorkspaceId = copyJob.WorkspaceId,
            FolderId = copyJob.FolderId
        };
    }
}
