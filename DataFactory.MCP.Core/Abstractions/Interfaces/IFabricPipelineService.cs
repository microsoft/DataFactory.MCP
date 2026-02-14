using DataFactory.MCP.Models.Pipeline;
using DataFactory.MCP.Models.Pipeline.Definition;

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
}
