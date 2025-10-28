using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Models;
using DataFactory.MCP.Models.Gateway;
using System.Text.Json;

namespace DataFactory.MCP.Tools;

[McpServerToolType]
public class GatewayTool
{
    private readonly IFabricGatewayService _gatewayService;

    public GatewayTool(IFabricGatewayService gatewayService)
    {
        _gatewayService = gatewayService;
    }

    [McpServerTool, Description(@"Lists all gateways the user has permission for, including on-premises, on-premises (personal mode), and virtual network gateways")]
    public async Task<string> ListGatewaysAsync(
        [Description("A token for retrieving the next page of results (optional)")] string? continuationToken = null)
    {
        try
        {
            var response = await _gatewayService.ListGatewaysAsync(continuationToken);

            if (!response.Value.Any())
            {
                return Messages.NoGatewaysFound;
            }

            var result = new
            {
                TotalCount = response.Value.Count,
                ContinuationToken = response.ContinuationToken,
                HasMoreResults = !string.IsNullOrEmpty(response.ContinuationToken),
                Gateways = response.Value.Select(g => g.ToFormattedInfo())
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return string.Format(Messages.AuthenticationErrorTemplate, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            return string.Format(Messages.ApiRequestFailedTemplate, ex.Message);
        }
        catch (Exception ex)
        {
            return string.Format(Messages.ErrorListingGatewaysTemplate, ex.Message);
        }
    }

    [McpServerTool, Description(@"Gets details about a specific gateway by its ID")]
    public async Task<string> GetGatewayAsync(
        [Description("The ID of the gateway to retrieve")] string gatewayId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(gatewayId))
            {
                return Messages.GatewayIdRequired;
            }

            var gateway = await _gatewayService.GetGatewayAsync(gatewayId);

            if (gateway == null)
            {
                return string.Format(Messages.GatewayNotFoundTemplate, gatewayId);
            }

            var result = gateway.ToFormattedInfo();
            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return string.Format(Messages.AuthenticationErrorTemplate, ex.Message);
        }
        catch (Exception ex)
        {
            return string.Format(Messages.ErrorRetrievingGatewayTemplate, ex.Message);
        }
    }

    [McpServerTool, Description(@"Creates a new VNet gateway in Microsoft Fabric")]
    public async Task<string> CreateVNetGatewayAsync(
        [Description("Display name for the gateway")] string displayName,
        [Description("The capacity ID where the gateway will be created")] string capacityId,
        [Description("Azure subscription ID containing the virtual network")] string subscriptionId,
        [Description("Azure resource group name containing the virtual network")] string resourceGroupName,
        [Description("Name of the virtual network")] string virtualNetworkName,
        [Description("Name of the subnet within the virtual network")] string subnetName,
        [Description("Number of minutes of inactivity before the gateway goes to sleep. Valid values: 30, 60, 90, 120, 150, 240, 360, 480, 720, 1440 (default: 120)")] int inactivityMinutesBeforeSleep = 120,
        [Description("Number of member gateways (default: 1)")] int numberOfMemberGateways = 1)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return "Error: displayName parameter is required.";
            }

            if (string.IsNullOrWhiteSpace(capacityId))
            {
                return "Error: capacityId parameter is required.";
            }

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

            if (string.IsNullOrWhiteSpace(subnetName))
            {
                return "Error: subnetName parameter is required.";
            }

            // Validate inactivityMinutesBeforeSleep
            var validValues = new[] { 30, 60, 90, 120, 150, 240, 360, 480, 720, 1440 };
            if (!validValues.Contains(inactivityMinutesBeforeSleep))
            {
                return $"Error: inactivityMinutesBeforeSleep must be one of: {string.Join(", ", validValues)}";
            }

            var request = new CreateVNetGatewayRequest
            {
                Type = "VirtualNetwork",
                DisplayName = displayName,
                CapacityId = capacityId,
                InactivityMinutesBeforeSleep = inactivityMinutesBeforeSleep,
                NumberOfMemberGateways = numberOfMemberGateways,
                VirtualNetworkAzureResource = new VirtualNetworkAzureResource
                {
                    SubscriptionId = subscriptionId,
                    ResourceGroupName = resourceGroupName,
                    VirtualNetworkName = virtualNetworkName,
                    SubnetName = subnetName
                }
            };

            var response = await _gatewayService.CreateVNetGatewayAsync(request);

            var result = new
            {
                message = $"Successfully created VNet gateway '{response.DisplayName}' with ID '{response.Id}'",
                gateway = new
                {
                    id = response.Id,
                    displayName = response.DisplayName,
                    type = response.Type,
                    connectivityStatus = response.ConnectivityStatus,
                    capacityId = response.CapacityId
                },
                configuration = new
                {
                    subscriptionId,
                    resourceGroupName,
                    virtualNetworkName,
                    subnetName,
                    inactivityMinutesBeforeSleep,
                    numberOfMemberGateways
                }
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return string.Format(Messages.AuthenticationErrorTemplate, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            return $"Error creating VNet gateway: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error creating VNet gateway '{displayName}': {ex.Message}";
        }
    }
}
