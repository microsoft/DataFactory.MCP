using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models.Gateway;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for interacting with Microsoft Fabric Gateways API.
/// Authentication is handled automatically by FabricAuthenticationHandler.
/// </summary>
public class FabricGatewayService : FabricServiceBase, IFabricGatewayService
{
    public FabricGatewayService(
        IHttpClientFactory httpClientFactory,
        ILogger<FabricGatewayService> logger,
        IValidationService validationService)
        : base(httpClientFactory, logger, validationService)
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
            ValidateGuids((gatewayId, nameof(gatewayId)));

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

    public async Task<CreateVirtualnetworkGatewayResponse> CreateVirtualnetworkGatewayAsync(CreateVirtualnetworkGatewayRequest request)
    {
        try
        {
            ValidateGuids((request.CapacityId, nameof(request.CapacityId)));

            Logger.LogInformation("Creating virtual network gateway '{DisplayName}' in capacity '{CapacityId}'",
                request.DisplayName, request.CapacityId);

            var response = await PostAsync<CreateVirtualnetworkGatewayResponse>("gateways", request);

            Logger.LogInformation("Successfully created virtual network gateway '{DisplayName}' with ID '{Id}'",
                response?.DisplayName, response?.Id);

            return response ?? new CreateVirtualnetworkGatewayResponse();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating virtual network gateway '{DisplayName}'", request.DisplayName);
            throw;
        }
    }
}
