using System.ComponentModel.DataAnnotations;
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
    [Required(ErrorMessage = "Subscription ID is required")]
    [RegularExpression(@"^[{(]?[0-9A-Fa-f]{8}[-]?([0-9A-Fa-f]{4}[-]?){3}[0-9A-Fa-f]{12}[)}]?$",
        ErrorMessage = "Subscription ID must be a valid GUID")]
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the resource group
    /// </summary>
    [JsonPropertyName("resourceGroupName")]
    [Required(ErrorMessage = "Resource group name is required")]
    [StringLength(90, MinimumLength = 1, ErrorMessage = "Resource group name must be between 1 and 90 characters")]
    public string ResourceGroupName { get; set; } = string.Empty;

    /// <summary>
    /// The name of the virtual network
    /// </summary>
    [JsonPropertyName("virtualNetworkName")]
    [Required(ErrorMessage = "Virtual network name is required")]
    [StringLength(80, MinimumLength = 1, ErrorMessage = "Virtual network name must be between 1 and 80 characters")]
    public string VirtualNetworkName { get; set; } = string.Empty;

    /// <summary>
    /// The name of the subnet
    /// </summary>
    [JsonPropertyName("subnetName")]
    [Required(ErrorMessage = "Subnet name is required")]
    [StringLength(80, MinimumLength = 1, ErrorMessage = "Subnet name must be between 1 and 80 characters")]
    public string SubnetName { get; set; } = string.Empty;
}
