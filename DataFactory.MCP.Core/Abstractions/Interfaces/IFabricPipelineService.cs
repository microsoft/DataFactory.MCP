using DataFactory.MCP.Models.Pipeline;
using DataFactory.MCP.Models.Pipeline.Definition;
using DataFactory.MCP.Models.Pipeline.Schedule;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Service for interacting with Microsoft Fabric Pipelines API
/// </summary>
public interface IFabricPipelineService
{
    /// <summary>
    /// Lists all pipelines from the specified workspace
    /// </summary>
    Task<ListPipelinesResponse> ListPipelinesAsync(
        string workspaceId,
        string? continuationToken = null);

    /// <summary>
    /// Creates a new pipeline in the specified workspace
    /// </summary>
    Task<CreatePipelineResponse> CreatePipelineAsync(
        string workspaceId,
        CreatePipelineRequest request);

    /// <summary>
    /// Gets pipeline metadata by ID
    /// </summary>
    Task<Pipeline> GetPipelineAsync(
        string workspaceId,
        string pipelineId);

    /// <summary>
    /// Gets the definition of a pipeline
    /// </summary>
    Task<PipelineDefinition> GetPipelineDefinitionAsync(
        string workspaceId,
        string pipelineId);

    /// <summary>
    /// Updates pipeline metadata (displayName, description)
    /// </summary>
    Task<Pipeline> UpdatePipelineAsync(
        string workspaceId,
        string pipelineId,
        UpdatePipelineRequest request);

    /// <summary>
    /// Updates a pipeline definition
    /// </summary>
    Task UpdatePipelineDefinitionAsync(
        string workspaceId,
        string pipelineId,
        PipelineDefinition definition);

    /// <summary>
    /// Runs a pipeline on demand. Returns the Location header URL for tracking the job instance.
    /// </summary>
    Task<string?> RunPipelineAsync(
        string workspaceId,
        string pipelineId,
        object? executionData = null);

    /// <summary>
    /// Gets the status of a pipeline job instance
    /// </summary>
    Task<ItemJobInstance> GetPipelineJobInstanceAsync(
        string workspaceId,
        string pipelineId,
        string jobInstanceId);

    /// <summary>
    /// Creates a new schedule for a pipeline
    /// </summary>
    Task<ItemSchedule> CreatePipelineScheduleAsync(
        string workspaceId,
        string pipelineId,
        CreateScheduleRequest request);

    /// <summary>
    /// Lists all schedules for a pipeline
    /// </summary>
    Task<ListSchedulesResponse> ListPipelineSchedulesAsync(
        string workspaceId,
        string pipelineId,
        string? continuationToken = null);
}
