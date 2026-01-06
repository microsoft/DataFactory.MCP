using DataFactory.MCP.Models.Capacity;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Service for interacting with Microsoft Fabric Capacities API
/// </summary>
public interface IFabricCapacityService
{
    /// <summary>
    /// Lists all capacities the user has permission for (either administrator or contributor)
    /// </summary>
    /// <param name="continuationToken">A token for retrieving the next page of results</param>
    /// <returns>List of capacities</returns>
    Task<ListCapacitiesResponse> ListCapacitiesAsync(string? continuationToken = null);
}