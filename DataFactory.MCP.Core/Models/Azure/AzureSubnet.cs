using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Azure;

/// <summary>
/// Represents an Azure subnet
/// </summary>
public class AzureSubnet
{
    /// <summary>
    /// The subnet ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The name of the subnet
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The properties of the subnet
    /// </summary>
    [JsonPropertyName("properties")]
    public AzureSubnetProperties? Properties { get; set; }
}

/// <summary>
/// Properties of an Azure subnet
/// </summary>
public class AzureSubnetProperties
{
    /// <summary>
    /// The address prefix of the subnet
    /// </summary>
    [JsonPropertyName("addressPrefix")]
    public string AddressPrefix { get; set; } = string.Empty;

    /// <summary>
    /// The provisioning state
    /// </summary>
    [JsonPropertyName("provisioningState")]
    public string ProvisioningState { get; set; } = string.Empty;

    /// <summary>
    /// Delegations for the subnet
    /// </summary>
    [JsonPropertyName("delegations")]
    public List<AzureSubnetDelegation>? Delegations { get; set; }
}

/// <summary>
/// Subnet delegation
/// </summary>
public class AzureSubnetDelegation
{
    /// <summary>
    /// The name of the delegation
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The service name
    /// </summary>
    [JsonPropertyName("properties")]
    public AzureSubnetDelegationProperties? Properties { get; set; }
}

/// <summary>
/// Properties of subnet delegation
/// </summary>
public class AzureSubnetDelegationProperties
{
    /// <summary>
    /// The service name
    /// </summary>
    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; } = string.Empty;
}

/// <summary>
/// Response for Azure subnets list API
/// </summary>
public class AzureSubnetsResponse
{
    /// <summary>
    /// The list of subnets
    /// </summary>
    [JsonPropertyName("value")]
    public List<AzureSubnet> Value { get; set; } = new();

    /// <summary>
    /// The URL for the next page of results
    /// </summary>
    [JsonPropertyName("nextLink")]
    public string? NextLink { get; set; }
}