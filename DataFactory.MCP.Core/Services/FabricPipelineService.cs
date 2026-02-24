using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Infrastructure.Http;
using DataFactory.MCP.Models.Pipeline;
using DataFactory.MCP.Models.Pipeline.Definition;
using DataFactory.MCP.Models.Pipeline.Schedule;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for interacting with Microsoft Fabric Pipelines API
/// </summary>
public class FabricPipelineService : FabricServiceBase, IFabricPipelineService
{
    public FabricPipelineService(
        IHttpClientFactory httpClientFactory,
        ILogger<FabricPipelineService> logger,
        IValidationService validationService)
        : base(httpClientFactory, logger, validationService)
    {
    }

    public async Task<ListPipelinesResponse> ListPipelinesAsync(
        string workspaceId,
        string? continuationToken = null)
    {
        try
        {
            ValidateGuids((workspaceId, nameof(workspaceId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/dataPipelines")
                .BuildEndpoint();
            Logger.LogInformation("Fetching pipelines from workspace {WorkspaceId}", workspaceId);

            var response = await GetAsync<ListPipelinesResponse>(endpoint, continuationToken);

            Logger.LogInformation("Successfully retrieved {Count} pipelines from workspace {WorkspaceId}",
                response?.Value?.Count ?? 0, workspaceId);
            return response ?? new ListPipelinesResponse();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching pipelines from workspace {WorkspaceId}", workspaceId);
            throw;
        }
    }

    public async Task<CreatePipelineResponse> CreatePipelineAsync(
        string workspaceId,
        CreatePipelineRequest request)
    {
        try
        {
            ValidateGuids((workspaceId, nameof(workspaceId)));
            ValidationService.ValidateAndThrow(request, nameof(request));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/dataPipelines")
                .BuildEndpoint();
            Logger.LogInformation("Creating pipeline '{DisplayName}' in workspace {WorkspaceId}",
                request.DisplayName, workspaceId);

            var createResponse = await PostAsync<CreatePipelineResponse>(endpoint, request);

            Logger.LogInformation("Successfully created pipeline '{DisplayName}' with ID {PipelineId} in workspace {WorkspaceId}",
                request.DisplayName, createResponse?.Id, workspaceId);

            return createResponse ?? new CreatePipelineResponse();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating pipeline '{DisplayName}' in workspace {WorkspaceId}",
                request?.DisplayName, workspaceId);
            throw;
        }
    }

    public async Task<Pipeline> GetPipelineAsync(
        string workspaceId,
        string pipelineId)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (pipelineId, nameof(pipelineId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/dataPipelines/{pipelineId}")
                .BuildEndpoint();
            Logger.LogInformation("Fetching pipeline {PipelineId} from workspace {WorkspaceId}",
                pipelineId, workspaceId);

            var pipeline = await GetAsync<Pipeline>(endpoint);

            Logger.LogInformation("Successfully retrieved pipeline {PipelineId}", pipelineId);
            return pipeline ?? throw new InvalidOperationException($"Pipeline {pipelineId} not found");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching pipeline {PipelineId} from workspace {WorkspaceId}",
                pipelineId, workspaceId);
            throw;
        }
    }

    public async Task<PipelineDefinition> GetPipelineDefinitionAsync(
        string workspaceId,
        string pipelineId)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (pipelineId, nameof(pipelineId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/items/{pipelineId}/getDefinition")
                .BuildEndpoint();
            Logger.LogInformation("Getting definition for pipeline {PipelineId} in workspace {WorkspaceId}",
                pipelineId, workspaceId);

            var emptyRequest = new { };
            var response = await PostAsync<GetPipelineDefinitionResponse>(endpoint, emptyRequest)
                           ?? throw new InvalidOperationException("Failed to get pipeline definition response");

            Logger.LogInformation("Successfully retrieved definition for pipeline {PipelineId}", pipelineId);
            return response.Definition;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting definition for pipeline {PipelineId} in workspace {WorkspaceId}",
                pipelineId, workspaceId);
            throw;
        }
    }

    public async Task<Pipeline> UpdatePipelineAsync(
        string workspaceId,
        string pipelineId,
        UpdatePipelineRequest request)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (pipelineId, nameof(pipelineId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/dataPipelines/{pipelineId}")
                .BuildEndpoint();
            Logger.LogInformation("Updating pipeline {PipelineId} in workspace {WorkspaceId}",
                pipelineId, workspaceId);

            var pipeline = await PatchAsync<Pipeline>(endpoint, request);

            Logger.LogInformation("Successfully updated pipeline {PipelineId}", pipelineId);
            return pipeline ?? throw new InvalidOperationException($"Failed to update pipeline {pipelineId}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating pipeline {PipelineId} in workspace {WorkspaceId}",
                pipelineId, workspaceId);
            throw;
        }
    }

    public async Task UpdatePipelineDefinitionAsync(
        string workspaceId,
        string pipelineId,
        PipelineDefinition definition)
    {
        ValidateGuids(
            (workspaceId, nameof(workspaceId)),
            (pipelineId, nameof(pipelineId)));

        var endpoint = FabricUrlBuilder.ForFabricApi()
            .WithLiteralPath($"workspaces/{workspaceId}/items/{pipelineId}/updateDefinition")
            .BuildEndpoint();
        var request = new UpdatePipelineDefinitionRequest { Definition = definition };

        Logger.LogInformation("Updating pipeline definition for {PipelineId}", pipelineId);

        var success = await PostNoContentAsync(endpoint, request);

        if (!success)
        {
            throw new HttpRequestException($"Failed to update pipeline definition for {pipelineId}");
        }

        Logger.LogInformation("Successfully updated pipeline definition for {PipelineId}", pipelineId);
    }

    private const string PipelineJobType = "Pipeline";

    public async Task<string?> RunPipelineAsync(
        string workspaceId,
        string pipelineId,
        object? executionData = null)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (pipelineId, nameof(pipelineId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/items/{pipelineId}/jobs/{PipelineJobType}/instances")
                .BuildEndpoint();
            Logger.LogInformation("Running pipeline {PipelineId} on demand in workspace {WorkspaceId}",
                pipelineId, workspaceId);

            var request = executionData != null ? new { executionData } : null;
            var location = await PostAndGetLocationAsync(endpoint, request);

            Logger.LogInformation("Pipeline {PipelineId} run triggered successfully. Location: {Location}",
                pipelineId, location);
            return location;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error running pipeline {PipelineId} in workspace {WorkspaceId}",
                pipelineId, workspaceId);
            throw;
        }
    }

    public async Task<ItemJobInstance> GetPipelineJobInstanceAsync(
        string workspaceId,
        string pipelineId,
        string jobInstanceId)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (pipelineId, nameof(pipelineId)),
                (jobInstanceId, nameof(jobInstanceId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/items/{pipelineId}/jobs/instances/{jobInstanceId}")
                .BuildEndpoint();
            Logger.LogInformation("Getting job instance {JobInstanceId} for pipeline {PipelineId} in workspace {WorkspaceId}",
                jobInstanceId, pipelineId, workspaceId);

            var jobInstance = await GetAsync<ItemJobInstance>(endpoint);

            Logger.LogInformation("Successfully retrieved job instance {JobInstanceId} with status {Status}",
                jobInstanceId, jobInstance?.Status);
            return jobInstance ?? throw new InvalidOperationException($"Job instance {jobInstanceId} not found");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting job instance {JobInstanceId} for pipeline {PipelineId} in workspace {WorkspaceId}",
                jobInstanceId, pipelineId, workspaceId);
            throw;
        }
    }

    public async Task<ItemSchedule> CreatePipelineScheduleAsync(
        string workspaceId,
        string pipelineId,
        CreateScheduleRequest request)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (pipelineId, nameof(pipelineId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/items/{pipelineId}/jobs/{PipelineJobType}/schedules")
                .BuildEndpoint();
            Logger.LogInformation("Creating schedule for pipeline {PipelineId} in workspace {WorkspaceId}",
                pipelineId, workspaceId);

            var schedule = await PostAsync<ItemSchedule>(endpoint, request);

            Logger.LogInformation("Successfully created schedule {ScheduleId} for pipeline {PipelineId}",
                schedule?.Id, pipelineId);
            return schedule ?? throw new InvalidOperationException("Failed to create pipeline schedule");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating schedule for pipeline {PipelineId} in workspace {WorkspaceId}",
                pipelineId, workspaceId);
            throw;
        }
    }

    public async Task<ListSchedulesResponse> ListPipelineSchedulesAsync(
        string workspaceId,
        string pipelineId,
        string? continuationToken = null)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (pipelineId, nameof(pipelineId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/items/{pipelineId}/jobs/{PipelineJobType}/schedules")
                .BuildEndpoint();
            Logger.LogInformation("Listing schedules for pipeline {PipelineId} in workspace {WorkspaceId}",
                pipelineId, workspaceId);

            var response = await GetAsync<ListSchedulesResponse>(endpoint, continuationToken);

            Logger.LogInformation("Successfully retrieved {Count} schedules for pipeline {PipelineId}",
                response?.Value?.Count ?? 0, pipelineId);
            return response ?? new ListSchedulesResponse();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error listing schedules for pipeline {PipelineId} in workspace {WorkspaceId}",
                pipelineId, workspaceId);
            throw;
        }
    }
}
