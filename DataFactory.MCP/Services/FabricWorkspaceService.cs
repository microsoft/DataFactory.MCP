using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models.Workspace;
using Microsoft.Extensions.Logging;
using System.Text;

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

            var url = BuildWorkspacesUrl(roles, continuationToken, preferWorkspaceSpecificEndpoints);

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

    private static string BuildWorkspacesUrl(string? roles, string? continuationToken, bool? preferWorkspaceSpecificEndpoints)
    {
        var url = new StringBuilder($"{BaseUrl}/workspaces");
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(roles))
        {
            queryParams.Add($"roles={Uri.EscapeDataString(roles)}");
        }

        if (!string.IsNullOrEmpty(continuationToken))
        {
            queryParams.Add($"continuationToken={Uri.EscapeDataString(continuationToken)}");
        }

        if (preferWorkspaceSpecificEndpoints.HasValue)
        {
            queryParams.Add($"preferWorkspaceSpecificEndpoints={preferWorkspaceSpecificEndpoints.Value.ToString().ToLower()}");
        }

        if (queryParams.Any())
        {
            url.Append("?");
            url.Append(string.Join("&", queryParams));
        }

        return url.ToString();
    }
}