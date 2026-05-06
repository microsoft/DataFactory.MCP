using Fabric.Mcp.Tools.DataFactory.Abstractions;
using Fabric.Mcp.Tools.DataFactory.Abstractions.Interfaces;
using Fabric.Mcp.Tools.DataFactory.Extensions;
using Fabric.Mcp.Tools.DataFactory.Infrastructure.Http;
using Fabric.Mcp.Tools.DataFactory.Models.Workspace;
using Microsoft.Extensions.Logging;

namespace Fabric.Mcp.Tools.DataFactory.Services;

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