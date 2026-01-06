using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models.Capacity;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for interacting with Microsoft Fabric Capacities API
/// </summary>
public class FabricCapacityService : FabricServiceBase, IFabricCapacityService
{
    public FabricCapacityService(
        IHttpClientFactory httpClientFactory,
        ILogger<FabricCapacityService> logger,
        IValidationService validationService)
        : base(httpClientFactory, logger, validationService)
    {
    }

    public async Task<ListCapacitiesResponse> ListCapacitiesAsync(string? continuationToken = null)
    {
        try
        {
            var capacitiesResponse = await GetAsync<ListCapacitiesResponse>("capacities", continuationToken);
            Logger.LogInformation("Successfully retrieved {Count} capacities", capacitiesResponse?.Value?.Count ?? 0);
            return capacitiesResponse ?? new ListCapacitiesResponse();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching capacities");
            throw;
        }
    }
}