using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using System.Text.Json;

namespace DataFactory.MCP.Tools;

[McpServerToolType]
public class ConnectionsTool
{
    private readonly IFabricConnectionService _connectionService;

    public ConnectionsTool(IFabricConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    [McpServerTool, Description(@"Lists all connections the user has permission for, including on-premises, virtual network and cloud connections")]
    public async Task<string> ListConnectionsAsync(
        [Description("A token for retrieving the next page of results (optional)")] string? continuationToken = null)
    {
        try
        {
            var response = await _connectionService.ListConnectionsAsync(continuationToken);

            if (!response.Value.Any())
            {
                return "No connections found. Response has an empty list.";
            }

            var result = new
            {
                TotalCount = response.Value.Count,
                ContinuationToken = response.ContinuationToken,
                HasMoreResults = !string.IsNullOrEmpty(response.ContinuationToken),
                Connections = response.Value.Select(c => c.ToFormattedInfo())
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
            return $"Error listing connections: {ex.Message}";
        }
    }

    [McpServerTool, Description(@"Gets details about a specific connection by its ID")]
    public async Task<string> GetConnectionAsync(
        [Description("The ID of the connection to retrieve")] string connectionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(connectionId))
            {
                return "Connection ID is required.";
            }

            var connection = await _connectionService.GetConnectionAsync(connectionId);

            if (connection == null)
            {
                return $"Connection with ID '{connectionId}' not found or you don't have permission to access it.";
            }

            var result = connection.ToFormattedInfo();
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
            return $"Error retrieving connection: {ex.Message}";
        }
    }
}