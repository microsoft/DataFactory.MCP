using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models.Workspace;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for interacting with Microsoft Fabric Workspaces API
/// </summary>
public class FabricWorkspaceService : FabricServiceBase, IFabricWorkspaceService
{
    public FabricWorkspaceService(
        ILogger<FabricWorkspaceService> logger,
        IAuthenticationService authService,
        IValidationService validationService)
        : base(logger, authService, validationService)
    {
    }

    public async Task<ListWorkspacesResponse> ListWorkspacesAsync(
        string? roles = null,
        string? continuationToken = null,
        bool? preferWorkspaceSpecificEndpoints = null)
    {
        try
        {
            await EnsureAuthenticationAsync();

            var url = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath("workspaces")
                .WithQueryParam("roles", roles)
                .WithContinuationToken(continuationToken)
                .WithQueryParam("preferWorkspaceSpecificEndpoints", preferWorkspaceSpecificEndpoints)
                .Build();

            Logger.LogInformation("Fetching workspaces from: {Url}", url);

            var response = await HttpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var workspacesResponse = System.Text.Json.JsonSerializer.Deserialize<ListWorkspacesResponse>(content, JsonOptions);

                Logger.LogInformation("Successfully retrieved {Count} workspaces", workspacesResponse?.Value?.Count ?? 0);
                return workspacesResponse ?? new ListWorkspacesResponse();
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
            Logger.LogError(ex, "Error fetching workspaces");
            throw;
        }
    }
}