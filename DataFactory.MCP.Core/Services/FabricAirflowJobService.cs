using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Infrastructure.Http;
using DataFactory.MCP.Models.AirflowJob;
using DataFactory.MCP.Models.AirflowJob.Definition;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for interacting with Microsoft Fabric Apache Airflow Jobs API
/// </summary>
public class FabricAirflowJobService : FabricServiceBase, IFabricAirflowJobService
{
    public FabricAirflowJobService(
        IHttpClientFactory httpClientFactory,
        ILogger<FabricAirflowJobService> logger,
        IValidationService validationService)
        : base(httpClientFactory, logger, validationService)
    {
    }

    public async Task<ListAirflowJobsResponse> ListAirflowJobsAsync(
        string workspaceId,
        string? continuationToken = null)
    {
        try
        {
            ValidateGuids((workspaceId, nameof(workspaceId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/apacheAirflowJobs")
                .BuildEndpoint();
            Logger.LogInformation("Fetching Apache Airflow Jobs from workspace {WorkspaceId}", workspaceId);

            var response = await GetAsync<ListAirflowJobsResponse>(endpoint, continuationToken);

            Logger.LogInformation("Successfully retrieved {Count} Apache Airflow Jobs from workspace {WorkspaceId}",
                response?.Value?.Count ?? 0, workspaceId);
            return response ?? new ListAirflowJobsResponse();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching Apache Airflow Jobs from workspace {WorkspaceId}", workspaceId);
            throw;
        }
    }

    public async Task<AirflowJob> CreateAirflowJobAsync(
        string workspaceId,
        CreateAirflowJobRequest request)
    {
        try
        {
            ValidateGuids((workspaceId, nameof(workspaceId)));
            ValidationService.ValidateAndThrow(request, nameof(request));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/apacheAirflowJobs")
                .BuildEndpoint();
            Logger.LogInformation("Creating Apache Airflow Job '{DisplayName}' in workspace {WorkspaceId}",
                request.DisplayName, workspaceId);

            var created = await PostAsync<AirflowJob>(endpoint, request);

            Logger.LogInformation("Successfully created Apache Airflow Job '{DisplayName}' with ID {AirflowJobId} in workspace {WorkspaceId}",
                request.DisplayName, created?.Id, workspaceId);

            return created ?? new AirflowJob();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating Apache Airflow Job '{DisplayName}' in workspace {WorkspaceId}",
                request?.DisplayName, workspaceId);
            throw;
        }
    }

    public async Task<AirflowJob> GetAirflowJobAsync(
        string workspaceId,
        string airflowJobId)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (airflowJobId, nameof(airflowJobId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/apacheAirflowJobs/{airflowJobId}")
                .BuildEndpoint();
            Logger.LogInformation("Fetching Apache Airflow Job {AirflowJobId} from workspace {WorkspaceId}",
                airflowJobId, workspaceId);

            var airflowJob = await GetAsync<AirflowJob>(endpoint);

            Logger.LogInformation("Successfully retrieved Apache Airflow Job {AirflowJobId}", airflowJobId);
            return airflowJob ?? throw new InvalidOperationException($"Apache Airflow Job {airflowJobId} not found");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching Apache Airflow Job {AirflowJobId} from workspace {WorkspaceId}",
                airflowJobId, workspaceId);
            throw;
        }
    }

    public async Task<AirflowJob> UpdateAirflowJobAsync(
        string workspaceId,
        string airflowJobId,
        UpdateAirflowJobRequest request)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (airflowJobId, nameof(airflowJobId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/apacheAirflowJobs/{airflowJobId}")
                .BuildEndpoint();
            Logger.LogInformation("Updating Apache Airflow Job {AirflowJobId} in workspace {WorkspaceId}",
                airflowJobId, workspaceId);

            var updated = await PatchAsync<AirflowJob>(endpoint, request);

            Logger.LogInformation("Successfully updated Apache Airflow Job {AirflowJobId}", airflowJobId);
            return updated ?? throw new InvalidOperationException($"Failed to update Apache Airflow Job {airflowJobId}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating Apache Airflow Job {AirflowJobId} in workspace {WorkspaceId}",
                airflowJobId, workspaceId);
            throw;
        }
    }

    public async Task DeleteAirflowJobAsync(
        string workspaceId,
        string airflowJobId,
        bool hardDelete = false)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (airflowJobId, nameof(airflowJobId)));

            var url = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/apacheAirflowJobs/{airflowJobId}")
                .WithQueryParam("hardDelete", hardDelete ? (bool?)true : null)
                .Build();
            Logger.LogInformation("Deleting Apache Airflow Job {AirflowJobId} from workspace {WorkspaceId} (hardDelete={HardDelete})",
                airflowJobId, workspaceId, hardDelete);

            var response = await HttpClient.DeleteAsync(url);
            await response.EnsureSuccessOrThrowAsync();

            Logger.LogInformation("Successfully deleted Apache Airflow Job {AirflowJobId}", airflowJobId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting Apache Airflow Job {AirflowJobId} from workspace {WorkspaceId}",
                airflowJobId, workspaceId);
            throw;
        }
    }

    public async Task<AirflowJobDefinition> GetAirflowJobDefinitionAsync(
        string workspaceId,
        string airflowJobId,
        string? format = null)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (airflowJobId, nameof(airflowJobId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/apacheAirflowJobs/{airflowJobId}/getDefinition")
                .WithQueryParam("format", format)
                .BuildEndpoint();
            Logger.LogInformation("Getting definition for Apache Airflow Job {AirflowJobId} in workspace {WorkspaceId}",
                airflowJobId, workspaceId);

            var emptyRequest = new { };
            var response = await PostAsync<GetAirflowJobDefinitionResponse>(endpoint, emptyRequest)
                           ?? throw new InvalidOperationException("Failed to get Apache Airflow Job definition response");

            Logger.LogInformation("Successfully retrieved definition for Apache Airflow Job {AirflowJobId}", airflowJobId);
            return response.Definition;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting definition for Apache Airflow Job {AirflowJobId} in workspace {WorkspaceId}",
                airflowJobId, workspaceId);
            throw;
        }
    }

    public async Task UpdateAirflowJobDefinitionAsync(
        string workspaceId,
        string airflowJobId,
        AirflowJobDefinition definition,
        bool updateMetadata = false)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (airflowJobId, nameof(airflowJobId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/apacheAirflowJobs/{airflowJobId}/updateDefinition")
                .WithQueryParam("updateMetadata", updateMetadata ? (bool?)true : null)
                .BuildEndpoint();
            Logger.LogInformation("Updating definition for Apache Airflow Job {AirflowJobId} in workspace {WorkspaceId}",
                airflowJobId, workspaceId);

            var request = new UpdateAirflowJobDefinitionRequest { Definition = definition };
            var success = await PostNoContentAsync(endpoint, request);

            if (!success)
            {
                throw new HttpRequestException($"Failed to update definition for Apache Airflow Job {airflowJobId}");
            }

            Logger.LogInformation("Successfully updated definition for Apache Airflow Job {AirflowJobId}", airflowJobId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating definition for Apache Airflow Job {AirflowJobId} in workspace {WorkspaceId}",
                airflowJobId, workspaceId);
            throw;
        }
    }
}
