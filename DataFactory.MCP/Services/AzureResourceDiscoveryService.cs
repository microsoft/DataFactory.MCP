using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models;
using DataFactory.MCP.Models.Azure;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for discovering Azure resources using Azure Resource Manager APIs
/// </summary>
public class AzureResourceDiscoveryService : IAzureResourceDiscoveryService
{
    private readonly IAuthenticationService _authService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureResourceDiscoveryService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AzureResourceDiscoveryService(
        IHttpClientFactory httpClientFactory,
        IAuthenticationService authService,
        ILogger<AzureResourceDiscoveryService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientNames.AzureResourceManager);
        _authService = authService;
        _logger = logger;
    }

    public async Task<List<AzureSubscription>> GetSubscriptionsAsync()
    {
        try
        {
            _logger.LogInformation("Getting Azure subscriptions");

            var token = await _authService.GetAccessTokenAsync(AzureAdConfiguration.AzureResourceManagerScopes);
            if (string.IsNullOrEmpty(token) || token.Contains("Error") || token.Contains("Failed"))
            {
                _logger.LogError("Failed to get Azure Resource Manager token: {Token}", token);
                return new List<AzureSubscription>();
            }

            var url = FabricUrlBuilder.ForAzureResourceManager()
                .WithLiteralPath("subscriptions")
                .WithApiVersion("2020-01-01")
                .Build();

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get subscriptions. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return new List<AzureSubscription>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var subscriptionsResponse = JsonSerializer.Deserialize<AzureSubscriptionsResponse>(content, JsonOptions);

            _logger.LogInformation("Successfully retrieved {Count} subscriptions", subscriptionsResponse?.Value?.Count ?? 0);
            return subscriptionsResponse?.Value ?? new List<AzureSubscription>();
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

            var token = await _authService.GetAccessTokenAsync(AzureAdConfiguration.AzureResourceManagerScopes);
            if (string.IsNullOrEmpty(token) || token.Contains("Error") || token.Contains("Failed"))
            {
                _logger.LogError("Failed to get Azure Resource Manager token: {Token}", token);
                return new List<AzureResourceGroup>();
            }

            var url = FabricUrlBuilder.ForAzureResourceManager()
                .WithLiteralPath($"subscriptions/{subscriptionId}/resourcegroups")
                .WithApiVersion("2021-04-01")
                .Build();

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get resource groups. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return new List<AzureResourceGroup>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var resourceGroupsResponse = JsonSerializer.Deserialize<AzureResourceGroupsResponse>(content, JsonOptions);

            _logger.LogInformation("Successfully retrieved {Count} resource groups", resourceGroupsResponse?.Value?.Count ?? 0);
            return resourceGroupsResponse?.Value ?? new List<AzureResourceGroup>();
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

            var token = await _authService.GetAccessTokenAsync(AzureAdConfiguration.AzureResourceManagerScopes);
            if (string.IsNullOrEmpty(token) || token.Contains("Error") || token.Contains("Failed"))
            {
                _logger.LogError("Failed to get Azure Resource Manager token: {Token}", token);
                return new List<AzureVirtualNetwork>();
            }

            var urlBuilder = FabricUrlBuilder.ForAzureResourceManager();
            if (!string.IsNullOrEmpty(resourceGroupName))
            {
                urlBuilder.WithLiteralPath($"subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/virtualNetworks");
            }
            else
            {
                urlBuilder.WithLiteralPath($"subscriptions/{subscriptionId}/providers/Microsoft.Network/virtualNetworks");
            }
            var url = urlBuilder.WithApiVersion("2023-04-01").Build();

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get virtual networks. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return new List<AzureVirtualNetwork>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var virtualNetworksResponse = JsonSerializer.Deserialize<AzureVirtualNetworksResponse>(content, JsonOptions);

            _logger.LogInformation("Successfully retrieved {Count} virtual networks", virtualNetworksResponse?.Value?.Count ?? 0);
            return virtualNetworksResponse?.Value ?? new List<AzureVirtualNetwork>();
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

            var token = await _authService.GetAccessTokenAsync(AzureAdConfiguration.AzureResourceManagerScopes);
            if (string.IsNullOrEmpty(token) || token.Contains("Error") || token.Contains("Failed"))
            {
                _logger.LogError("Failed to get Azure Resource Manager token: {Token}", token);
                return new List<AzureSubnet>();
            }

            var url = FabricUrlBuilder.ForAzureResourceManager()
                .WithLiteralPath($"subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/virtualNetworks/{virtualNetworkName}/subnets")
                .WithApiVersion("2023-04-01")
                .Build();

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get subnets. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return new List<AzureSubnet>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var subnetsResponse = JsonSerializer.Deserialize<AzureSubnetsResponse>(content, JsonOptions);

            _logger.LogInformation("Successfully retrieved {Count} subnets", subnetsResponse?.Value?.Count ?? 0);
            return subnetsResponse?.Value ?? new List<AzureSubnet>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subnets for VNet {VirtualNetworkName}", virtualNetworkName);
            return new List<AzureSubnet>();
        }
    }
}