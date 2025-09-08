using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
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
                return "No gateways found. Make sure you have the required permissions (Gateway.Read.All or Gateway.ReadWrite.All).";
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

    [McpServerTool, Description(@"Gets details about a specific gateway by its ID")]
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

            var result = gateway.ToFormattedInfo();
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
}
