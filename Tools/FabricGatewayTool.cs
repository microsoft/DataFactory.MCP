using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models.Gateway;
using System.Text.Json;

namespace DataFactory.MCP.Tools;

[McpServerToolType]
public class FabricGatewayTool
{
    private readonly IFabricGatewayService _gatewayService;

    public FabricGatewayTool(IFabricGatewayService gatewayService)
    {
        _gatewayService = gatewayService;
    }

    [McpServerTool, Description(@"Lists all Microsoft Fabric gateways the user has permission for, including on-premises, on-premises (personal mode), and virtual network gateways")]
    public async Task<string> ListGatewaysAsync(
        [Description("A token for retrieving the next page of results (optional)")] string? continuationToken = null)
    {
        try
        {
            var response = await _gatewayService.ListGatewaysAsync(continuationToken);

            if (!response.Value.Any())
            {
                return "No gateways found. Make sure you have the required permissions (Gateway.Read.All or Gateway.ReadWrite.All).";
            }

            var result = new
            {
                TotalCount = response.Value.Count,
                ContinuationToken = response.ContinuationToken,
                HasMoreResults = !string.IsNullOrEmpty(response.ContinuationToken),
                Gateways = response.Value.Select(g => FormatGatewayInfo(g))
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return $"Authentication error: {ex.Message}";
        }
        catch (HttpRequestException ex)
        {
            return $"API request failed: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error listing gateways: {ex.Message}";
        }
    }

    [McpServerTool, Description(@"Gets details about a specific Microsoft Fabric gateway by its ID")]
    public async Task<string> GetGatewayAsync(
        [Description("The ID of the gateway to retrieve")] string gatewayId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(gatewayId))
            {
                return "Gateway ID is required.";
            }

            var gateway = await _gatewayService.GetGatewayAsync(gatewayId);

            if (gateway == null)
            {
                return $"Gateway with ID '{gatewayId}' not found or you don't have permission to access it.";
            }

            var result = FormatGatewayInfo(gateway);
            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return $"Authentication error: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error retrieving gateway: {ex.Message}";
        }
    }

    [McpServerTool, Description(@"Lists all gateways in a summarized table format for easy viewing")]
    public async Task<string> ListGatewaysSummaryAsync()
    {
        try
        {
            var response = await _gatewayService.ListGatewaysAsync();

            if (!response.Value.Any())
            {
                return "No gateways found.";
            }

            var summary = "Microsoft Fabric Gateways Summary:\n" +
                         "=" + new string('=', 80) + "\n\n";

            foreach (var gateway in response.Value)
            {
                summary += FormatGatewaySummary(gateway) + "\n";
            }

            summary += $"\nTotal: {response.Value.Count} gateway(s)";

            if (!string.IsNullOrEmpty(response.ContinuationToken))
            {
                summary += "\n(More results available - use ListGatewaysAsync with continuationToken)";
            }

            return summary;
        }
        catch (UnauthorizedAccessException ex)
        {
            return $"Authentication error: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error listing gateways summary: {ex.Message}";
        }
    }

    private static object FormatGatewayInfo(Gateway gateway)
    {
        var baseInfo = new
        {
            Id = gateway.Id,
            Type = gateway.Type
        };

        return gateway switch
        {
            OnPremisesGateway onPrem => new
            {
                baseInfo.Id,
                baseInfo.Type,
                DisplayName = onPrem.DisplayName,
                Version = onPrem.Version,
                NumberOfMembers = onPrem.NumberOfMemberGateways,
                LoadBalancing = onPrem.LoadBalancingSetting,
                AllowCloudRefresh = onPrem.AllowCloudConnectionRefresh,
                AllowCustomConnectors = onPrem.AllowCustomConnectors,
                PublicKey = new
                {
                    Exponent = onPrem.PublicKey.Exponent,
                    Modulus = onPrem.PublicKey.Modulus.Length > 20
                        ? onPrem.PublicKey.Modulus[..20] + "..."
                        : onPrem.PublicKey.Modulus
                }
            },
            OnPremisesGatewayPersonal personal => new
            {
                baseInfo.Id,
                baseInfo.Type,
                Version = personal.Version,
                PublicKey = new
                {
                    Exponent = personal.PublicKey.Exponent,
                    Modulus = personal.PublicKey.Modulus.Length > 20
                        ? personal.PublicKey.Modulus[..20] + "..."
                        : personal.PublicKey.Modulus
                }
            },
            VirtualNetworkGateway vnet => new
            {
                baseInfo.Id,
                baseInfo.Type,
                DisplayName = vnet.DisplayName,
                CapacityId = vnet.CapacityId,
                NumberOfMembers = vnet.NumberOfMemberGateways,
                InactivityMinutes = vnet.InactivityMinutesBeforeSleep,
                VirtualNetwork = new
                {
                    SubscriptionId = vnet.VirtualNetworkAzureResource.SubscriptionId,
                    ResourceGroup = vnet.VirtualNetworkAzureResource.ResourceGroupName,
                    VNetName = vnet.VirtualNetworkAzureResource.VirtualNetworkName,
                    Subnet = vnet.VirtualNetworkAzureResource.SubnetName
                }
            },
            _ => baseInfo
        };
    }

    private static string FormatGatewaySummary(Gateway gateway)
    {
        return gateway switch
        {
            OnPremisesGateway onPrem =>
                $"🏢 {onPrem.DisplayName}\n" +
                $"   ID: {onPrem.Id}\n" +
                $"   Type: {onPrem.Type} | Version: {onPrem.Version}\n" +
                $"   Members: {onPrem.NumberOfMemberGateways} | Load Balancing: {onPrem.LoadBalancingSetting}\n" +
                $"   Cloud Refresh: {(onPrem.AllowCloudConnectionRefresh ? "✓" : "✗")} | Custom Connectors: {(onPrem.AllowCustomConnectors ? "✓" : "✗")}\n",

            OnPremisesGatewayPersonal personal =>
                $"👤 Personal Gateway\n" +
                $"   ID: {personal.Id}\n" +
                $"   Type: {personal.Type} | Version: {personal.Version}\n",

            VirtualNetworkGateway vnet =>
                $"🌐 {vnet.DisplayName}\n" +
                $"   ID: {vnet.Id}\n" +
                $"   Type: {vnet.Type} | Members: {vnet.NumberOfMemberGateways}\n" +
                $"   Capacity: {vnet.CapacityId}\n" +
                $"   VNet: {vnet.VirtualNetworkAzureResource.VirtualNetworkName} ({vnet.VirtualNetworkAzureResource.ResourceGroupName})\n" +
                $"   Auto-sleep: {vnet.InactivityMinutesBeforeSleep} minutes\n",

            _ => $"Gateway ID: {gateway.Id} | Type: {gateway.Type}\n"
        };
    }
}
