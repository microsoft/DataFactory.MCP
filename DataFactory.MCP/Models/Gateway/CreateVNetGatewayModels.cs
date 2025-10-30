using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Gateway;

/// <summary>
/// Request model for creating a VNet gateway in Microsoft Fabric
/// </summary>
public class CreateVNetGatewayRequest
{
    /// <summary>
    /// The type of gateway. For VNet gateways, this should be "VirtualNetwork"
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "VirtualNetwork";

    /// <summary>
    /// Display name for the gateway
    /// </summary>
    [JsonPropertyName("displayName")]
    [Required(ErrorMessage = "Display name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Display name must be between 1 and 100 characters")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The capacity ID where the gateway will be created
    /// </summary>
    [JsonPropertyName("capacityId")]
    [Required(ErrorMessage = "Capacity ID is required")]
    [RegularExpression(@"^[{(]?[0-9A-Fa-f]{8}[-]?([0-9A-Fa-f]{4}[-]?){3}[0-9A-Fa-f]{12}[)}]?$",
        ErrorMessage = "Capacity ID must be a valid GUID")]
    public string CapacityId { get; set; } = string.Empty;

    /// <summary>
    /// Azure virtual network resource configuration
    /// </summary>
    [JsonPropertyName("virtualNetworkAzureResource")]
    [Required(ErrorMessage = "Virtual network Azure resource configuration is required")]
    public VirtualNetworkAzureResource VirtualNetworkAzureResource { get; set; } = new();

    /// <summary>
    /// Number of minutes of inactivity before the gateway goes to sleep.
    /// Must be one of: 30, 60, 90, 120, 150, 240, 360, 480, 720, 1440
    /// </summary>
    [JsonPropertyName("inactivityMinutesBeforeSleep")]
    [Range(30, 1440, ErrorMessage = "Inactivity minutes must be between 30 and 1440")]
    [InactivityValidation]
    public int InactivityMinutesBeforeSleep { get; set; } = 120;

    /// <summary>
    /// Number of member gateways
    /// </summary>
    [JsonPropertyName("numberOfMemberGateways")]
    [Range(1, 10, ErrorMessage = "Number of member gateways must be between 1 and 10")]
    public int NumberOfMemberGateways { get; set; } = 1;
}

/// <summary>
/// Custom validation attribute for inactivity minutes
/// </summary>
public class InactivityValidationAttribute : ValidationAttribute
{
    private static readonly int[] ValidValues = { 30, 60, 90, 120, 150, 240, 360, 480, 720, 1440 };

    public override bool IsValid(object? value)
    {
        if (value is int intValue)
        {
            return ValidValues.Contains(intValue);
        }
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be one of: {string.Join(", ", ValidValues)}";
    }
}

/// <summary>
/// Response model for VNet gateway creation
/// </summary>
public class CreateVNetGatewayResponse
{
    /// <summary>
    /// ID of the created gateway
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the created gateway
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Type of the gateway
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the gateway
    /// </summary>
    [JsonPropertyName("connectivityStatus")]
    public string ConnectivityStatus { get; set; } = string.Empty;

    /// <summary>
    /// Capacity ID where the gateway was created
    /// </summary>
    [JsonPropertyName("capacityId")]
    public string CapacityId { get; set; } = string.Empty;
}