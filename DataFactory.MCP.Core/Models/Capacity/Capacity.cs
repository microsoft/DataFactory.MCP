using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Capacity;

/// <summary>
/// A capacity object representing a Microsoft Fabric capacity
/// </summary>
public class Capacity
{
    /// <summary>
    /// The capacity ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The capacity display name
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The capacity SKU (e.g., F4, F8, F16, FT1)
    /// </summary>
    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// The Azure region where the capacity was provisioned
    /// </summary>
    [JsonPropertyName("region")]
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// The capacity state
    /// </summary>
    [JsonPropertyName("state")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CapacityState State { get; set; }
}