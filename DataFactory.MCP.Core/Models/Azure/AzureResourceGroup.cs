using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Azure;

/// <summary>
/// Represents an Azure resource group
/// </summary>
public class AzureResourceGroup
{
    /// <summary>
    /// The resource group ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The name of the resource group
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The location of the resource group
    /// </summary>
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// The provisioning state of the resource group
    /// </summary>
    [JsonPropertyName("properties")]
    public AzureResourceGroupProperties? Properties { get; set; }
}

/// <summary>
/// Properties of an Azure resource group
/// </summary>
public class AzureResourceGroupProperties
{
    /// <summary>
    /// The provisioning state
    /// </summary>
    [JsonPropertyName("provisioningState")]
    public string ProvisioningState { get; set; } = string.Empty;
}

/// <summary>
/// Response for Azure resource groups list API
/// </summary>
public class AzureResourceGroupsResponse
{
    /// <summary>
    /// The list of resource groups
    /// </summary>
    [JsonPropertyName("value")]
    public List<AzureResourceGroup> Value { get; set; } = new();

    /// <summary>
    /// The URL for the next page of results
    /// </summary>
    [JsonPropertyName("nextLink")]
    public string? NextLink { get; set; }
}