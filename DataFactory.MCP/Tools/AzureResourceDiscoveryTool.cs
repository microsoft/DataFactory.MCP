using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using System.Text.Json;

namespace DataFactory.MCP.Tools;

[McpServerToolType]
public class AzureResourceDiscoveryTool
{
    private readonly IAzureResourceDiscoveryService _azureResourceService;

    public AzureResourceDiscoveryTool(IAzureResourceDiscoveryService azureResourceService)
    {
        _azureResourceService = azureResourceService;
    }

    [McpServerTool, Description(@"Get all Azure subscriptions the authenticated user has access to")]
    public async Task<string> GetAzureSubscriptionsAsync()
    {
        try
        {
            var subscriptions = await _azureResourceService.GetSubscriptionsAsync();

            if (!subscriptions.Any())
            {
                return "No Azure subscriptions found or failed to retrieve subscriptions. Please ensure you have the correct permissions and are authenticated with Azure Resource Manager.";
            }

            var result = new
            {
                totalCount = subscriptions.Count,
                subscriptions = subscriptions.Select(s => new
                {
                    subscriptionId = s.SubscriptionId,
                    displayName = s.DisplayName,
                    state = s.State,
                    tenantId = s.TenantId
                }).ToList()
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"Error retrieving Azure subscriptions: {ex.Message}";
        }
    }

    [McpServerTool, Description(@"Get all resource groups in an Azure subscription")]
    public async Task<string> GetAzureResourceGroupsAsync(string subscriptionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return "Error: subscriptionId parameter is required.";
            }

            var resourceGroups = await _azureResourceService.GetResourceGroupsAsync(subscriptionId);

            if (!resourceGroups.Any())
            {
                return $"No resource groups found in subscription {subscriptionId} or failed to retrieve resource groups.";
            }

            var result = new
            {
                subscriptionId,
                totalCount = resourceGroups.Count,
                resourceGroups = resourceGroups.Select(rg => new
                {
                    id = rg.Id,
                    name = rg.Name,
                    location = rg.Location,
                    provisioningState = rg.Properties?.ProvisioningState
                }).ToList()
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"Error retrieving resource groups for subscription {subscriptionId}: {ex.Message}";
        }
    }

    [McpServerTool, Description(@"Get all virtual networks in an Azure subscription, optionally filtered by resource group")]
    public async Task<string> GetAzureVirtualNetworksAsync(string subscriptionId, string? resourceGroupName = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return "Error: subscriptionId parameter is required.";
            }

            var virtualNetworks = await _azureResourceService.GetVirtualNetworksAsync(subscriptionId, resourceGroupName);

            if (!virtualNetworks.Any())
            {
                var scope = string.IsNullOrEmpty(resourceGroupName) ? $"subscription {subscriptionId}" : $"resource group {resourceGroupName}";
                return $"No virtual networks found in {scope} or failed to retrieve virtual networks.";
            }

            var result = new
            {
                subscriptionId,
                resourceGroupName,
                totalCount = virtualNetworks.Count,
                virtualNetworks = virtualNetworks.Select(vnet => new
                {
                    id = vnet.Id,
                    name = vnet.Name,
                    location = vnet.Location,
                    provisioningState = vnet.Properties?.ProvisioningState,
                    addressPrefixes = vnet.Properties?.AddressSpace?.AddressPrefixes ?? new List<string>(),
                    subnetCount = vnet.Properties?.Subnets?.Count ?? 0
                }).ToList()
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"Error retrieving virtual networks for subscription {subscriptionId}: {ex.Message}";
        }
    }

    [McpServerTool, Description(@"Get all subnets in a specific Azure virtual network")]
    public async Task<string> GetAzureSubnetsAsync(string subscriptionId, string resourceGroupName, string virtualNetworkName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return "Error: subscriptionId parameter is required.";
            }

            if (string.IsNullOrWhiteSpace(resourceGroupName))
            {
                return "Error: resourceGroupName parameter is required.";
            }

            if (string.IsNullOrWhiteSpace(virtualNetworkName))
            {
                return "Error: virtualNetworkName parameter is required.";
            }

            var subnets = await _azureResourceService.GetSubnetsAsync(subscriptionId, resourceGroupName, virtualNetworkName);

            if (!subnets.Any())
            {
                return $"No subnets found in virtual network {virtualNetworkName} or failed to retrieve subnets.";
            }

            var result = new
            {
                subscriptionId,
                resourceGroupName,
                virtualNetworkName,
                totalCount = subnets.Count,
                subnets = subnets.Select(subnet => new
                {
                    id = subnet.Id,
                    name = subnet.Name,
                    addressPrefix = subnet.Properties?.AddressPrefix,
                    provisioningState = subnet.Properties?.ProvisioningState,
                    delegations = subnet.Properties?.Delegations?.Select(d => new
                    {
                        name = d.Name,
                        serviceName = d.Properties?.ServiceName
                    }) ?? Enumerable.Empty<object>()
                }).ToList()
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"Error retrieving subnets for virtual network {virtualNetworkName}: {ex.Message}";
        }
    }
}