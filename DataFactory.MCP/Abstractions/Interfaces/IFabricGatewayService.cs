using DataFactory.MCP.Models.Gateway;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Service for interacting with Microsoft Fabric Gateways API
/// </summary>
public interface IFabricGatewayService
{
    /// <summary>
    /// Lists all gateways the user has permission for
    /// </summary>
    /// <param name="continuationToken">A token for retrieving the next page of results</param>
    /// <returns>List of gateways</returns>
    Task<ListGatewaysResponse> ListGatewaysAsync(string? continuationToken = null);

    /// <summary>
    /// Gets a specific gateway by ID
    /// </summary>
    /// <param name="gatewayId">The ID of the gateway</param>
    /// <returns>Gateway details if found</returns>
    Task<Gateway?> GetGatewayAsync(string gatewayId);
}
