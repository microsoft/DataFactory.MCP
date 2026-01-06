using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Capacity;

/// <summary>
/// A capacity state. Additional capacity states may be added over time.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CapacityState
{
    /// <summary>
    /// The capacity is ready to use
    /// </summary>
    Active,

    /// <summary>
    /// The capacity can't be used
    /// </summary>
    Inactive
}