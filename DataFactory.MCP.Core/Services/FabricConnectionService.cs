using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models.Connection;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for interacting with Microsoft Fabric Connections API.
/// Authentication is handled automatically by FabricAuthenticationHandler.
/// </summary>
public class FabricConnectionService : FabricServiceBase, IFabricConnectionService
{
    public FabricConnectionService(
        IHttpClientFactory httpClientFactory,
        ILogger<FabricConnectionService> logger,
        IValidationService validationService)
        : base(httpClientFactory, logger, validationService)
    {
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
            ValidateGuids((connectionId, nameof(connectionId)));

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