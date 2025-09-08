using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Gateway;

/// <summary>
/// The properties of a Virtual Network Azure resource
/// </summary>
public class VirtualNetworkAzureResource
{
    /// <summary>
    /// The subscription ID
    /// </summary>
    [JsonPropertyName("subscriptionId")]
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the resource group
    /// </summary>
    [JsonPropertyName("resourceGroupName")]
    public string ResourceGroupName { get; set; } = string.Empty;

    /// <summary>
    /// The name of the virtual network
    /// </summary>
    [JsonPropertyName("virtualNetworkName")]
    public string VirtualNetworkName { get; set; } = string.Empty;

    /// <summary>
    /// The name of the subnet
    /// </summary>
    [JsonPropertyName("subnetName")]
    public string SubnetName { get; set; } = string.Empty;
}
