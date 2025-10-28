using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Azure;

/// <summary>
/// Represents an Azure virtual network
/// </summary>
public class AzureVirtualNetwork
{
    /// <summary>
    /// The virtual network ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The name of the virtual network
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The location of the virtual network
    /// </summary>
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// The properties of the virtual network
    /// </summary>
    [JsonPropertyName("properties")]
    public AzureVirtualNetworkProperties? Properties { get; set; }
}

/// <summary>
/// Properties of an Azure virtual network
/// </summary>
public class AzureVirtualNetworkProperties
{
    /// <summary>
    /// The address space of the virtual network
    /// </summary>
    [JsonPropertyName("addressSpace")]
    public AzureAddressSpace? AddressSpace { get; set; }

    /// <summary>
    /// The subnets in the virtual network
    /// </summary>
    [JsonPropertyName("subnets")]
    public List<AzureSubnet>? Subnets { get; set; }

    /// <summary>
    /// The provisioning state
    /// </summary>
    [JsonPropertyName("provisioningState")]
    public string ProvisioningState { get; set; } = string.Empty;
}

/// <summary>
/// Address space of a virtual network
/// </summary>
public class AzureAddressSpace
{
    /// <summary>
    /// The address prefixes
    /// </summary>
    [JsonPropertyName("addressPrefixes")]
    public List<string> AddressPrefixes { get; set; } = new();
}

/// <summary>
/// Response for Azure virtual networks list API
/// </summary>
public class AzureVirtualNetworksResponse
{
    /// <summary>
    /// The list of virtual networks
    /// </summary>
    [JsonPropertyName("value")]
    public List<AzureVirtualNetwork> Value { get; set; } = new();

    /// <summary>
    /// The URL for the next page of results
    /// </summary>
    [JsonPropertyName("nextLink")]
    public string? NextLink { get; set; }
}