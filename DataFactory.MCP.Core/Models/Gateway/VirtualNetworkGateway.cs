using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Gateway;

/// <summary>
/// Virtual network gateway
/// </summary>
public class VirtualNetworkGateway : Gateway
{
    /// <summary>
    /// The display name of the virtual network gateway
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The object ID of the Fabric license capacity
    /// </summary>
    [JsonPropertyName("capacityId")]
    public string CapacityId { get; set; } = string.Empty;

    /// <summary>
    /// The Azure virtual network resource
    /// </summary>
    [JsonPropertyName("virtualNetworkAzureResource")]
    public VirtualNetworkAzureResource VirtualNetworkAzureResource { get; set; } = new();

    /// <summary>
    /// The minutes of inactivity before the virtual network gateway goes into auto-sleep
    /// </summary>
    [JsonPropertyName("inactivityMinutesBeforeSleep")]
    public int InactivityMinutesBeforeSleep { get; set; }

    /// <summary>
    /// The number of member gateways
    /// </summary>
    [JsonPropertyName("numberOfMemberGateways")]
    public int NumberOfMemberGateways { get; set; }
}
