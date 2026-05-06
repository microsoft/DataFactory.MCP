using Fabric.Mcp.Tools.DataFactory.Models.Pipeline;

namespace Fabric.Mcp.Tools.DataFactory.Extensions;

/// <summary>
/// Extension methods for Pipeline model transformations.
/// </summary>
public static class PipelineExtensions
{
    /// <summary>
    /// Formats a Pipeline object for MCP API responses.
    /// </summary>
    public static object ToFormattedInfo(this Pipeline pipeline)
    {
        return new
        {
            Id = pipeline.Id,
            DisplayName = pipeline.DisplayName,
            Description = pipeline.Description,
            Type = pipeline.Type,
            WorkspaceId = pipeline.WorkspaceId,
            FolderId = pipeline.FolderId
        };
    }
}
