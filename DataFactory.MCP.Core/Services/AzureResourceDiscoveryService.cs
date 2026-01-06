using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Configuration;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Infrastructure.Http;
using DataFactory.MCP.Models.Azure;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for discovering Azure resources using Azure Resource Manager APIs.
/// Authentication is handled automatically by the AzureResourceManagerAuthenticationHandler in the HTTP pipeline.
/// </summary>
public class AzureResourceDiscoveryService : IAzureResourceDiscoveryService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureResourceDiscoveryService> _logger;

    private static JsonSerializerOptions JsonOptions => JsonSerializerOptionsProvider.CaseInsensitive;

    public AzureResourceDiscoveryService(
        IHttpClientFactory httpClientFactory,
        ILogger<AzureResourceDiscoveryService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientNames.AzureResourceManager);
        _logger = logger;
    }

    public async Task<List<AzureSubscription>> GetSubscriptionsAsync()
    {
        try
        {
            _logger.LogInformation("Getting Azure subscriptions");

            var url = FabricUrlBuilder.ForAzureResourceManager()
                .WithLiteralPath("subscriptions")
                .WithApiVersion(ApiVersions.AzureResourceManager.Subscriptions)
                .Build();

            var response = await _httpClient.GetAsync(url);
            var subscriptionsResponse = await response.ReadAsJsonOrDefaultAsync(
                new AzureSubscriptionsResponse(), JsonOptions);

            _logger.LogInformation("Successfully retrieved {Count} subscriptions", subscriptionsResponse.Value?.Count ?? 0);
            return subscriptionsResponse.Value ?? new List<AzureSubscription>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Azure subscriptions");
            return new List<AzureSubscription>();
        }
    }

    public async Task<List<AzureResourceGroup>> GetResourceGroupsAsync(string subscriptionId)
    {
        try
        {
            _logger.LogInformation("Getting resource groups for subscription {SubscriptionId}", subscriptionId);

            var url = FabricUrlBuilder.ForAzureResourceManager()
                .WithLiteralPath($"subscriptions/{subscriptionId}/resourcegroups")
                .WithApiVersion(ApiVersions.AzureResourceManager.ResourceGroups)
                .Build();

            var response = await _httpClient.GetAsync(url);
            var resourceGroupsResponse = await response.ReadAsJsonOrDefaultAsync(
                new AzureResourceGroupsResponse(), JsonOptions);

            _logger.LogInformation("Successfully retrieved {Count} resource groups", resourceGroupsResponse.Value?.Count ?? 0);
            return resourceGroupsResponse.Value ?? new List<AzureResourceGroup>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resource groups for subscription {SubscriptionId}", subscriptionId);
            return new List<AzureResourceGroup>();
        }
    }

    public async Task<List<AzureVirtualNetwork>> GetVirtualNetworksAsync(string subscriptionId, string? resourceGroupName = null)
    {
        try
        {
            _logger.LogInformation("Getting virtual networks for subscription {SubscriptionId}, resource group {ResourceGroupName}",
                subscriptionId, resourceGroupName ?? "all");

            var urlBuilder = FabricUrlBuilder.ForAzureResourceManager();
            if (!string.IsNullOrEmpty(resourceGroupName))
            {
                urlBuilder.WithLiteralPath($"subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/virtualNetworks");
            }
            else
            {
                urlBuilder.WithLiteralPath($"subscriptions/{subscriptionId}/providers/Microsoft.Network/virtualNetworks");
            }
            var url = urlBuilder.WithApiVersion(ApiVersions.AzureResourceManager.Network).Build();

            var response = await _httpClient.GetAsync(url);
            var virtualNetworksResponse = await response.ReadAsJsonOrDefaultAsync(
                new AzureVirtualNetworksResponse(), JsonOptions);

            _logger.LogInformation("Successfully retrieved {Count} virtual networks", virtualNetworksResponse.Value?.Count ?? 0);
            return virtualNetworksResponse.Value ?? new List<AzureVirtualNetwork>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting virtual networks for subscription {SubscriptionId}", subscriptionId);
            return new List<AzureVirtualNetwork>();
        }
    }

    public async Task<List<AzureSubnet>> GetSubnetsAsync(string subscriptionId, string resourceGroupName, string virtualNetworkName)
    {
        try
        {
            _logger.LogInformation("Getting subnets for VNet {VirtualNetworkName} in resource group {ResourceGroupName}",
                virtualNetworkName, resourceGroupName);

            var url = FabricUrlBuilder.ForAzureResourceManager()
                .WithLiteralPath($"subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/virtualNetworks/{virtualNetworkName}/subnets")
                .WithApiVersion(ApiVersions.AzureResourceManager.Network)
                .Build();

            var response = await _httpClient.GetAsync(url);
            var subnetsResponse = await response.ReadAsJsonOrDefaultAsync(
                new AzureSubnetsResponse(), JsonOptions);

            _logger.LogInformation("Successfully retrieved {Count} subnets", subnetsResponse.Value?.Count ?? 0);
            return subnetsResponse.Value ?? new List<AzureSubnet>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subnets for VNet {VirtualNetworkName}", virtualNetworkName);
            return new List<AzureSubnet>();
        }
    }
}