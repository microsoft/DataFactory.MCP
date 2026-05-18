using DataFactory.MCP.Models.AirflowJob;

namespace DataFactory.MCP.Extensions;

/// <summary>
/// Extension methods for Apache Airflow Job model transformations.
/// </summary>
public static class AirflowJobExtensions
{
    /// <summary>
    /// Formats an AirflowJob object for MCP API responses.
    /// </summary>
    public static object ToFormattedInfo(this AirflowJob airflowJob)
    {
        return new
        {
            Id = airflowJob.Id,
            DisplayName = airflowJob.DisplayName,
            Description = airflowJob.Description,
            Type = airflowJob.Type,
            WorkspaceId = airflowJob.WorkspaceId,
            FolderId = airflowJob.FolderId
        };
    }
}
