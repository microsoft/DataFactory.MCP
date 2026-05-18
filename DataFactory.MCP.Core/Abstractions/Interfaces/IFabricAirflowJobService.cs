using DataFactory.MCP.Models.AirflowJob;
using DataFactory.MCP.Models.AirflowJob.Definition;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Service for interacting with Microsoft Fabric Apache Airflow Jobs API
/// </summary>
public interface IFabricAirflowJobService
{
    /// <summary>
    /// Lists all Apache Airflow Jobs from the specified workspace
    /// </summary>
    Task<ListAirflowJobsResponse> ListAirflowJobsAsync(
        string workspaceId,
        string? continuationToken = null);

    /// <summary>
    /// Creates a new Apache Airflow Job in the specified workspace
    /// </summary>
    Task<AirflowJob> CreateAirflowJobAsync(
        string workspaceId,
        CreateAirflowJobRequest request);

    /// <summary>
    /// Gets Apache Airflow Job metadata by ID
    /// </summary>
    Task<AirflowJob> GetAirflowJobAsync(
        string workspaceId,
        string airflowJobId);

    /// <summary>
    /// Updates Apache Airflow Job metadata (displayName, description)
    /// </summary>
    Task<AirflowJob> UpdateAirflowJobAsync(
        string workspaceId,
        string airflowJobId,
        UpdateAirflowJobRequest request);

    /// <summary>
    /// Deletes an Apache Airflow Job
    /// </summary>
    Task DeleteAirflowJobAsync(
        string workspaceId,
        string airflowJobId,
        bool hardDelete = false);

    /// <summary>
    /// Gets the definition of an Apache Airflow Job
    /// </summary>
    Task<AirflowJobDefinition> GetAirflowJobDefinitionAsync(
        string workspaceId,
        string airflowJobId,
        string? format = null);

    /// <summary>
    /// Updates the definition of an Apache Airflow Job
    /// </summary>
    Task UpdateAirflowJobDefinitionAsync(
        string workspaceId,
        string airflowJobId,
        AirflowJobDefinition definition,
        bool updateMetadata = false);
}
