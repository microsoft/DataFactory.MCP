using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Gateway;

/// <summary>
/// On-premises gateway
/// </summary>
public class OnPremisesGateway : Gateway
{
    /// <summary>
    /// The display name of the on-premises gateway
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The public key of the primary gateway member
    /// </summary>
    [JsonPropertyName("publicKey")]
    public PublicKey PublicKey { get; set; } = new();

    /// <summary>
    /// The version of the installed primary gateway member
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// The number of gateway members in the on-premises gateway
    /// </summary>
    [JsonPropertyName("numberOfMemberGateways")]
    public int NumberOfMemberGateways { get; set; }

    /// <summary>
    /// The load balancing setting of the on-premises gateway
    /// </summary>
    [JsonPropertyName("loadBalancingSetting")]
    public string LoadBalancingSetting { get; set; } = string.Empty;

    /// <summary>
    /// Whether to allow cloud connections to refresh through this on-premises gateway
    /// </summary>
    [JsonPropertyName("allowCloudConnectionRefresh")]
    public bool AllowCloudConnectionRefresh { get; set; }

    /// <summary>
    /// Whether to allow custom connectors to be used with this on-premises gateway
    /// </summary>
    [JsonPropertyName("allowCustomConnectors")]
    public bool AllowCustomConnectors { get; set; }
}
