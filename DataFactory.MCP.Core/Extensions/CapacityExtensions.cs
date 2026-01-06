using DataFactory.MCP.Models.Capacity;

namespace DataFactory.MCP.Extensions;

/// <summary>
/// Extension methods for Capacity model transformations.
/// </summary>
public static class CapacityExtensions
{
    /// <summary>
    /// Formats a Capacity object for MCP API responses.
    /// Provides consistent output format and human-readable information.
    /// </summary>
    /// <param name="capacity">The capacity object to format</param>
    /// <returns>Formatted object ready for JSON serialization</returns>
    public static object ToFormattedInfo(this Capacity capacity)
    {
        return new
        {
            Id = capacity.Id,
            DisplayName = capacity.DisplayName,
            Sku = capacity.Sku,
            SkuDescription = GetSkuDescription(capacity.Sku),
            Region = capacity.Region,
            State = capacity.State.ToString(),
            Status = GetStatusDescription(capacity.State),
            IsActive = capacity.State == CapacityState.Active
        };
    }

    /// <summary>
    /// Formats a list of capacities for MCP API responses with summary information.
    /// </summary>
    /// <param name="capacities">The list of capacities to format</param>
    /// <returns>Formatted response with capacities and summary</returns>
    public static object ToFormattedList(this IEnumerable<Capacity> capacities)
    {
        var capacityList = capacities.ToList();

        var groupedByState = capacityList.GroupBy(c => c.State).ToDictionary(g => g.Key.ToString(), g => g.Count());
        var groupedBySku = capacityList.GroupBy(c => c.Sku).ToDictionary(g => g.Key, g => g.Count());
        var groupedByRegion = capacityList.GroupBy(c => c.Region).ToDictionary(g => g.Key, g => g.Count());

        return new
        {
            Capacities = capacityList.Select(c => c.ToFormattedInfo()),
            Summary = new
            {
                TotalCount = capacityList.Count,
                ByState = groupedByState,
                BySku = groupedBySku,
                ByRegion = groupedByRegion,
                ActiveCount = capacityList.Count(c => c.State == CapacityState.Active),
                InactiveCount = capacityList.Count(c => c.State == CapacityState.Inactive)
            }
        };
    }

    /// <summary>
    /// Gets a human-readable description for the capacity SKU
    /// </summary>
    /// <param name="sku">The SKU code</param>
    /// <returns>Human-readable SKU description</returns>
    private static string GetSkuDescription(string sku)
    {
        return sku.ToUpper() switch
        {
            "FT1" => "Free Trial (FT1)",
            "F2" => "Fabric F2 - 2 vCores",
            "F4" => "Fabric F4 - 4 vCores",
            "F8" => "Fabric F8 - 8 vCores",
            "F16" => "Fabric F16 - 16 vCores",
            "F32" => "Fabric F32 - 32 vCores",
            "F64" => "Fabric F64 - 64 vCores",
            "F128" => "Fabric F128 - 128 vCores",
            "F256" => "Fabric F256 - 256 vCores",
            "F512" => "Fabric F512 - 512 vCores",
            _ => $"Fabric {sku}"
        };
    }

    /// <summary>
    /// Gets a human-readable status description
    /// </summary>
    /// <param name="state">The capacity state</param>
    /// <returns>Human-readable status description</returns>
    private static string GetStatusDescription(CapacityState state)
    {
        return state switch
        {
            CapacityState.Active => "Ready to use",
            CapacityState.Inactive => "Cannot be used",
            _ => "Unknown status"
        };
    }
}