using DataFactory.MCP.Models.Azure;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Service for discovering Azure resources using Azure Resource Manager APIs
/// </summary>
public interface IAzureResourceDiscoveryService
{
    /// <summary>
    /// Get all Azure subscriptions the user has access to
    /// </summary>
    Task<List<AzureSubscription>> GetSubscriptionsAsync();

    /// <summary>
    /// Get all resource groups in a subscription
    /// </summary>
    /// <param name="subscriptionId">The subscription ID</param>
    Task<List<AzureResourceGroup>> GetResourceGroupsAsync(string subscriptionId);

    /// <summary>
    /// Get all virtual networks in a subscription
    /// </summary>
    /// <param name="subscriptionId">The subscription ID</param>
    /// <param name="resourceGroupName">Optional resource group name to filter by</param>
    Task<List<AzureVirtualNetwork>> GetVirtualNetworksAsync(string subscriptionId, string? resourceGroupName = null);

    /// <summary>
    /// Get all subnets in a virtual network
    /// </summary>
    /// <param name="subscriptionId">The subscription ID</param>
    /// <param name="resourceGroupName">The resource group name</param>
    /// <param name="virtualNetworkName">The virtual network name</param>
    Task<List<AzureSubnet>> GetSubnetsAsync(string subscriptionId, string resourceGroupName, string virtualNetworkName);
}