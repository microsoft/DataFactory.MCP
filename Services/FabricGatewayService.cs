using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models;
using DataFactory.MCP.Models.Gateway;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for interacting with Microsoft Fabric Gateways API
/// </summary>
public class FabricGatewayService : FabricServiceBase, IFabricGatewayService
{
    public FabricGatewayService(
        HttpClient httpClient,
        ILogger<FabricGatewayService> logger,
        IAuthenticationService authService)
        : base(httpClient, logger, authService)
    {
    }

    public async Task<ListGatewaysResponse> ListGatewaysAsync(string? continuationToken = null)
    {
        try
        {
            var gatewaysResponse = await GetAsync<ListGatewaysResponse>("gateways", continuationToken);
            Logger.LogInformation("Successfully retrieved {Count} gateways", gatewaysResponse?.Value?.Count ?? 0);
            return gatewaysResponse ?? new ListGatewaysResponse();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching gateways");
            throw;
        }
    }

    public async Task<Gateway?> GetGatewayAsync(string gatewayId)
    {
        try
        {
            // The Fabric API doesn't have a direct get gateway by ID endpoint,
            // so we'll list all gateways and find the specific one
            var allGateways = await ListGatewaysAsync();
            return allGateways.Value.FirstOrDefault(g => g.Id.Equals(gatewayId, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching gateway {GatewayId}", gatewayId);
            throw;
        }
    }
}
