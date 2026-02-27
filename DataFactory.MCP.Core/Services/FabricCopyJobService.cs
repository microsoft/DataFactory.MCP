using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Infrastructure.Http;
using DataFactory.MCP.Models.CopyJob;
using DataFactory.MCP.Models.CopyJob.Definition;
using DataFactory.MCP.Models.Pipeline;
using DataFactory.MCP.Models.Pipeline.Schedule;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for interacting with Microsoft Fabric Copy Jobs API
/// </summary>
public class FabricCopyJobService : FabricServiceBase, IFabricCopyJobService
{
    public FabricCopyJobService(
        IHttpClientFactory httpClientFactory,
        ILogger<FabricCopyJobService> logger,
        IValidationService validationService)
        : base(httpClientFactory, logger, validationService)
    {
    }

    public async Task<ListCopyJobsResponse> ListCopyJobsAsync(
        string workspaceId,
        string? continuationToken = null)
    {
        try
        {
            ValidateGuids((workspaceId, nameof(workspaceId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/copyJobs")
                .BuildEndpoint();
            Logger.LogInformation("Fetching copy jobs from workspace {WorkspaceId}", workspaceId);

            var response = await GetAsync<ListCopyJobsResponse>(endpoint, continuationToken);

            Logger.LogInformation("Successfully retrieved {Count} copy jobs from workspace {WorkspaceId}",
                response?.Value?.Count ?? 0, workspaceId);
            return response ?? new ListCopyJobsResponse();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching copy jobs from workspace {WorkspaceId}", workspaceId);
            throw;
        }
    }

    public async Task<CreateCopyJobResponse> CreateCopyJobAsync(
        string workspaceId,
        CreateCopyJobRequest request)
    {
        try
        {
            ValidateGuids((workspaceId, nameof(workspaceId)));
            ValidationService.ValidateAndThrow(request, nameof(request));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/copyJobs")
                .BuildEndpoint();
            Logger.LogInformation("Creating copy job '{DisplayName}' in workspace {WorkspaceId}",
                request.DisplayName, workspaceId);

            var createResponse = await PostAsync<CreateCopyJobResponse>(endpoint, request);

            Logger.LogInformation("Successfully created copy job '{DisplayName}' with ID {CopyJobId} in workspace {WorkspaceId}",
                request.DisplayName, createResponse?.Id, workspaceId);

            return createResponse ?? new CreateCopyJobResponse();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating copy job '{DisplayName}' in workspace {WorkspaceId}",
                request?.DisplayName, workspaceId);
            throw;
        }
    }

    public async Task<CopyJob> GetCopyJobAsync(
        string workspaceId,
        string copyJobId)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (copyJobId, nameof(copyJobId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/copyJobs/{copyJobId}")
                .BuildEndpoint();
            Logger.LogInformation("Fetching copy job {CopyJobId} from workspace {WorkspaceId}",
                copyJobId, workspaceId);

            var copyJob = await GetAsync<CopyJob>(endpoint);

            Logger.LogInformation("Successfully retrieved copy job {CopyJobId}", copyJobId);
            return copyJob ?? throw new InvalidOperationException($"Copy job {copyJobId} not found");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching copy job {CopyJobId} from workspace {WorkspaceId}",
                copyJobId, workspaceId);
            throw;
        }
    }

    public async Task<CopyJobDefinition> GetCopyJobDefinitionAsync(
        string workspaceId,
        string copyJobId)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (copyJobId, nameof(copyJobId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/items/{copyJobId}/getDefinition")
                .BuildEndpoint();
            Logger.LogInformation("Getting definition for copy job {CopyJobId} in workspace {WorkspaceId}",
                copyJobId, workspaceId);

            var emptyRequest = new { };
            var response = await PostAsync<GetCopyJobDefinitionResponse>(endpoint, emptyRequest)
                           ?? throw new InvalidOperationException("Failed to get copy job definition response");

            Logger.LogInformation("Successfully retrieved definition for copy job {CopyJobId}", copyJobId);
            return response.Definition;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting definition for copy job {CopyJobId} in workspace {WorkspaceId}",
                copyJobId, workspaceId);
            throw;
        }
    }

    public async Task<CopyJob> UpdateCopyJobAsync(
        string workspaceId,
        string copyJobId,
        UpdateCopyJobRequest request)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (copyJobId, nameof(copyJobId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/copyJobs/{copyJobId}")
                .BuildEndpoint();
            Logger.LogInformation("Updating copy job {CopyJobId} in workspace {WorkspaceId}",
                copyJobId, workspaceId);

            var copyJob = await PatchAsync<CopyJob>(endpoint, request);

            Logger.LogInformation("Successfully updated copy job {CopyJobId}", copyJobId);
            return copyJob ?? throw new InvalidOperationException($"Failed to update copy job {copyJobId}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating copy job {CopyJobId} in workspace {WorkspaceId}",
                copyJobId, workspaceId);
            throw;
        }
    }

    public async Task UpdateCopyJobDefinitionAsync(
        string workspaceId,
        string copyJobId,
        CopyJobDefinition definition)
    {
        ValidateGuids(
            (workspaceId, nameof(workspaceId)),
            (copyJobId, nameof(copyJobId)));

        var endpoint = FabricUrlBuilder.ForFabricApi()
            .WithLiteralPath($"workspaces/{workspaceId}/items/{copyJobId}/updateDefinition")
            .BuildEndpoint();
        var request = new UpdateCopyJobDefinitionRequest { Definition = definition };

        Logger.LogInformation("Updating copy job definition for {CopyJobId}", copyJobId);

        var success = await PostNoContentAsync(endpoint, request);

        if (!success)
        {
            throw new HttpRequestException($"Failed to update copy job definition for {copyJobId}");
        }

        Logger.LogInformation("Successfully updated copy job definition for {CopyJobId}", copyJobId);
    }

    private const string CopyJobType = "CopyJob";

    public async Task<string?> RunCopyJobAsync(
        string workspaceId,
        string copyJobId,
        object? executionData = null)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (copyJobId, nameof(copyJobId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/items/{copyJobId}/jobs/{CopyJobType}/instances")
                .BuildEndpoint();
            Logger.LogInformation("Running copy job {CopyJobId} on demand in workspace {WorkspaceId}",
                copyJobId, workspaceId);

            var request = executionData != null ? new { executionData } : null;
            var location = await PostAndGetLocationAsync(endpoint, request);

            Logger.LogInformation("Copy job {CopyJobId} run triggered successfully. Location: {Location}",
                copyJobId, location);
            return location;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error running copy job {CopyJobId} in workspace {WorkspaceId}",
                copyJobId, workspaceId);
            throw;
        }
    }

    public async Task<ItemJobInstance> GetCopyJobInstanceAsync(
        string workspaceId,
        string copyJobId,
        string jobInstanceId)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (copyJobId, nameof(copyJobId)),
                (jobInstanceId, nameof(jobInstanceId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/items/{copyJobId}/jobs/instances/{jobInstanceId}")
                .BuildEndpoint();
            Logger.LogInformation("Getting job instance {JobInstanceId} for copy job {CopyJobId} in workspace {WorkspaceId}",
                jobInstanceId, copyJobId, workspaceId);

            var jobInstance = await GetAsync<ItemJobInstance>(endpoint);

            Logger.LogInformation("Successfully retrieved job instance {JobInstanceId} with status {Status}",
                jobInstanceId, jobInstance?.Status);
            return jobInstance ?? throw new InvalidOperationException($"Job instance {jobInstanceId} not found");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting job instance {JobInstanceId} for copy job {CopyJobId} in workspace {WorkspaceId}",
                jobInstanceId, copyJobId, workspaceId);
            throw;
        }
    }

    public async Task<ItemSchedule> CreateCopyJobScheduleAsync(
        string workspaceId,
        string copyJobId,
        CreateScheduleRequest request)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (copyJobId, nameof(copyJobId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/items/{copyJobId}/jobs/{CopyJobType}/schedules")
                .BuildEndpoint();
            Logger.LogInformation("Creating schedule for copy job {CopyJobId} in workspace {WorkspaceId}",
                copyJobId, workspaceId);

            var schedule = await PostAsync<ItemSchedule>(endpoint, request);

            Logger.LogInformation("Successfully created schedule {ScheduleId} for copy job {CopyJobId}",
                schedule?.Id, copyJobId);
            return schedule ?? throw new InvalidOperationException("Failed to create copy job schedule");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating schedule for copy job {CopyJobId} in workspace {WorkspaceId}",
                copyJobId, workspaceId);
            throw;
        }
    }

    public async Task<ListSchedulesResponse> ListCopyJobSchedulesAsync(
        string workspaceId,
        string copyJobId,
        string? continuationToken = null)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (copyJobId, nameof(copyJobId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/items/{copyJobId}/jobs/{CopyJobType}/schedules")
                .BuildEndpoint();
            Logger.LogInformation("Listing schedules for copy job {CopyJobId} in workspace {WorkspaceId}",
                copyJobId, workspaceId);

            var response = await GetAsync<ListSchedulesResponse>(endpoint, continuationToken);

            Logger.LogInformation("Successfully retrieved {Count} schedules for copy job {CopyJobId}",
                response?.Value?.Count ?? 0, copyJobId);
            return response ?? new ListSchedulesResponse();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error listing schedules for copy job {CopyJobId} in workspace {WorkspaceId}",
                copyJobId, workspaceId);
            throw;
        }
    }
}
