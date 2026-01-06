using DataFactory.MCP.Models.Connection;
using DataFactory.MCP.Models.Connection.Create;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Service for interacting with Microsoft Fabric Connections API
/// </summary>
public interface IFabricConnectionService
{
    /// <summary>
    /// Lists all connections the user has permission for
    /// </summary>
    /// <param name="continuationToken">A token for retrieving the next page of results</param>
    /// <returns>List of connections</returns>
    Task<ListConnectionsResponse> ListConnectionsAsync(string? continuationToken = null);

    /// <summary>
    /// Gets a specific connection by ID
    /// </summary>
    /// <param name="connectionId">The ID of the connection</param>
    /// <returns>Connection details if found</returns>
    Task<Connection?> GetConnectionAsync(string connectionId);

    /// <summary>
    /// Creates a new cloud connection
    /// </summary>
    /// <param name="request">The create cloud connection request</param>
    /// <returns>The created connection</returns>
    Task<ShareableCloudConnection> CreateCloudConnectionAsync(CreateCloudConnectionRequest request);

    /// <summary>
    /// Creates a new virtual network gateway connection
    /// </summary>
    /// <param name="request">The create virtual network gateway connection request</param>
    /// <returns>The created connection</returns>
    Task<VirtualNetworkGatewayConnection> CreateVirtualNetworkGatewayConnectionAsync(CreateVirtualNetworkGatewayConnectionRequest request);
}