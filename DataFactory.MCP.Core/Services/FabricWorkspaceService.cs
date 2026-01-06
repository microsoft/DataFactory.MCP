using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Infrastructure.Http;
using DataFactory.MCP.Models.Workspace;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for interacting with Microsoft Fabric Workspaces API.
/// Authentication is handled automatically by FabricAuthenticationHandler.
/// </summary>
public class FabricWorkspaceService : FabricServiceBase, IFabricWorkspaceService
{
    public FabricWorkspaceService(
        IHttpClientFactory httpClientFactory,
        ILogger<FabricWorkspaceService> logger,
        IValidationService validationService)
        : base(httpClientFactory, logger, validationService)
    {
    }

    public async Task<ListWorkspacesResponse> ListWorkspacesAsync(
        string? roles = null,
        string? continuationToken = null,
        bool? preferWorkspaceSpecificEndpoints = null)
    {
        try
        {
            var url = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath("workspaces")
                .WithQueryParam("roles", roles)
                .WithContinuationToken(continuationToken)
                .WithQueryParam("preferWorkspaceSpecificEndpoints", preferWorkspaceSpecificEndpoints)
                .Build();

            Logger.LogInformation("Fetching workspaces from: {Url}", url);

            var response = await HttpClient.GetAsync(url);
            var workspacesResponse = await response.ReadAsJsonAsync<ListWorkspacesResponse>(JsonOptions);

            Logger.LogInformation("Successfully retrieved {Count} workspaces", workspacesResponse?.Value?.Count ?? 0);
            return workspacesResponse ?? new ListWorkspacesResponse();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching workspaces");
            throw;
        }
    }
}