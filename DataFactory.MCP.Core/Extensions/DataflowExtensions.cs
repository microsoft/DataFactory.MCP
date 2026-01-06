using DataFactory.MCP.Models.Dataflow;
using DataFactory.MCP.Models.Dataflow.Definition;

namespace DataFactory.MCP.Extensions;

/// <summary>
/// Extension methods for Dataflow model transformations.
/// </summary>
public static class DataflowExtensions
{
    /// <summary>
    /// Formats a Dataflow object for MCP API responses.
    /// Provides consistent output format and handles optional properties appropriately.
    /// </summary>
    /// <param name="dataflow">The dataflow object to format</param>
    /// <returns>Formatted object ready for JSON serialization</returns>
    public static object ToFormattedInfo(this Dataflow dataflow)
    {
        var formattedInfo = new
        {
            Id = dataflow.Id,
            DisplayName = dataflow.DisplayName,
            Description = dataflow.Description,
            Type = dataflow.Type,
            WorkspaceId = dataflow.WorkspaceId,
            FolderId = dataflow.FolderId,
            Tags = dataflow.Tags?.Select(tag => new { tag.Id, tag.DisplayName }),
            Properties = dataflow.Properties != null ? new
            {
                IsParametric = dataflow.Properties.IsParametric
            } : null
        };

        return formattedInfo;
    }
}