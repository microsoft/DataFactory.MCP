using DataFactory.MCP.Models.Connection;

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
    /// Creates a new connection via the Fabric Connections API
    /// </summary>
    /// <param name="request">The create connection request</param>
    /// <returns>The created connection</returns>
    Task<Connection?> CreateConnectionAsync(CreateConnectionRequest request);

    /// <summary>
    /// Lists supported connection types from the Fabric Connections API.
    /// Fetches all pages and returns the combined result.
    /// </summary>
    /// <param name="gatewayId">Optional gateway ID to filter by</param>
    /// <returns>List of supported connection types with creation methods and parameters</returns>
    Task<ListSupportedConnectionTypesResponse> ListSupportedConnectionTypesAsync(string? gatewayId = null);
}