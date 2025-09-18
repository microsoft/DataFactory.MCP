using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models.Connection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for interacting with Microsoft Fabric Connections API
/// </summary>
public class FabricConnectionService : FabricServiceBase, IFabricConnectionService
{
    public FabricConnectionService(
        ILogger<FabricConnectionService> logger,
        IAuthenticationService authService)
        : base(logger, authService)
    {
        // Add the custom connection converter to handle polymorphic deserialization
        JsonOptions.Converters.Add(new ConnectionJsonConverter());
    }

    public async Task<ListConnectionsResponse> ListConnectionsAsync(string? continuationToken = null)
    {
        try
        {
            var connectionsResponse = await GetAsync<ListConnectionsResponse>("connections", continuationToken);
            Logger.LogInformation("Successfully retrieved {Count} connections", connectionsResponse?.Value?.Count ?? 0);
            return connectionsResponse ?? new ListConnectionsResponse();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching connections");
            throw;
        }
    }

    public async Task<Connection?> GetConnectionAsync(string connectionId)
    {
        try
        {
            // The Fabric API doesn't have a direct get connection by ID endpoint,
            // so we'll list all connections and find the specific one
            var allConnections = await ListConnectionsAsync();
            return allConnections.Value.FirstOrDefault(c => c.Id.Equals(connectionId, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching connection {ConnectionId}", connectionId);
            throw;
        }
    }
}