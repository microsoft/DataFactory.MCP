using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models.Dataflow;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for interacting with Microsoft Fabric Dataflows API
/// </summary>
public class FabricDataflowService : FabricServiceBase, IFabricDataflowService
{
    private readonly IValidationService _validationService;

    public FabricDataflowService(
        ILogger<FabricDataflowService> logger,
        IAuthenticationService authService,
        IValidationService validationService)
        : base(logger, authService)
    {
        _validationService = validationService;
    }

    public async Task<ListDataflowsResponse> ListDataflowsAsync(
        string workspaceId,
        string? continuationToken = null)
    {
        try
        {
            _validationService.ValidateGuid(workspaceId, nameof(workspaceId));

            await EnsureAuthenticationAsync();

            var url = BuildDataflowsUrl(workspaceId, continuationToken);

            Logger.LogInformation("Fetching dataflows from workspace {WorkspaceId}: {Url}", workspaceId, url);

            var response = await HttpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var dataflowsResponse = System.Text.Json.JsonSerializer.Deserialize<ListDataflowsResponse>(content, JsonOptions);

                Logger.LogInformation("Successfully retrieved {Count} dataflows from workspace {WorkspaceId}",
                    dataflowsResponse?.Value?.Count ?? 0, workspaceId);
                return dataflowsResponse ?? new ListDataflowsResponse();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogError("API request failed. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);

                throw new HttpRequestException($"API request failed: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching dataflows from workspace {WorkspaceId}", workspaceId);
            throw;
        }
    }

    public async Task<CreateDataflowResponse> CreateDataflowAsync(
        string workspaceId,
        CreateDataflowRequest request)
    {
        try
        {
            _validationService.ValidateGuid(workspaceId, nameof(workspaceId));
            _validationService.ValidateAndThrow(request, nameof(request));

            await EnsureAuthenticationAsync();

            var url = $"{BaseUrl}/workspaces/{workspaceId}/dataflows";
            var jsonContent = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            Logger.LogInformation("Creating dataflow '{DisplayName}' in workspace {WorkspaceId}: {Url}",
                request.DisplayName, workspaceId, url);

            var response = await HttpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var createResponse = JsonSerializer.Deserialize<CreateDataflowResponse>(responseContent, JsonOptions);

                Logger.LogInformation("Successfully created dataflow '{DisplayName}' with ID {DataflowId} in workspace {WorkspaceId}",
                    request.DisplayName, createResponse?.Id, workspaceId);

                return createResponse ?? new CreateDataflowResponse();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogError("Failed to create dataflow. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);

                throw new HttpRequestException($"Failed to create dataflow: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating dataflow '{DisplayName}' in workspace {WorkspaceId}",
                request?.DisplayName, workspaceId);
            throw;
        }
    }

    private static string BuildDataflowsUrl(string workspaceId, string? continuationToken)
    {
        var url = new StringBuilder($"{BaseUrl}/workspaces/{workspaceId}/dataflows");
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(continuationToken))
        {
            queryParams.Add($"continuationToken={Uri.EscapeDataString(continuationToken)}");
        }

        if (queryParams.Any())
        {
            url.Append("?");
            url.Append(string.Join("&", queryParams));
        }

        return url.ToString();
    }
}