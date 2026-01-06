using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;

namespace DataFactory.MCP.Tools;

[McpServerToolType]
public class AzureResourceDiscoveryTool
{
    private readonly IAzureResourceDiscoveryService _azureResourceService;
    private readonly IValidationService _validationService;

    public AzureResourceDiscoveryTool(IAzureResourceDiscoveryService azureResourceService, IValidationService validationService)
    {
        _azureResourceService = azureResourceService;
        _validationService = validationService;
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

            return result.ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("retrieving Azure subscriptions").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Get all resource groups in an Azure subscription")]
    public async Task<string> GetAzureResourceGroupsAsync(string subscriptionId)
    {
        try
        {
            _validationService.ValidateRequiredString(subscriptionId, nameof(subscriptionId));

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

            return result.ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError($"retrieving resource groups for subscription {subscriptionId}").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Get all virtual networks in an Azure subscription, optionally filtered by resource group")]
    public async Task<string> GetAzureVirtualNetworksAsync(string subscriptionId, string? resourceGroupName = null)
    {
        try
        {
            _validationService.ValidateRequiredString(subscriptionId, nameof(subscriptionId));

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

            return result.ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError($"retrieving virtual networks for subscription {subscriptionId}").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Get all subnets in a specific Azure virtual network")]
    public async Task<string> GetAzureSubnetsAsync(string subscriptionId, string resourceGroupName, string virtualNetworkName)
    {
        try
        {
            _validationService.ValidateRequiredString(subscriptionId, nameof(subscriptionId));
            _validationService.ValidateRequiredString(resourceGroupName, nameof(resourceGroupName));
            _validationService.ValidateRequiredString(virtualNetworkName, nameof(virtualNetworkName));

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

            return result.ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError($"retrieving subnets for virtual network {virtualNetworkName}").ToMcpJson();
        }
    }
}