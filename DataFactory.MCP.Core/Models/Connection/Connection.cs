using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Connection;

/// <summary>
/// Base connection class
/// </summary>
public abstract class Connection
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("connectivityType")]
    public ConnectivityType ConnectivityType { get; set; }

    [JsonPropertyName("connectionDetails")]
    public ListConnectionDetails ConnectionDetails { get; set; } = new();

    [JsonPropertyName("privacyLevel")]
    public PrivacyLevel? PrivacyLevel { get; set; }

    [JsonPropertyName("credentialDetails")]
    public ListCredentialDetails? CredentialDetails { get; set; }
}

/// <summary>
/// A connection that connects through the cloud and can be shared
/// </summary>
public class ShareableCloudConnection : Connection
{
    [JsonPropertyName("allowConnectionUsageInGateway")]
    public bool AllowConnectionUsageInGateway { get; set; }
}

/// <summary>
/// A connection that connects through the cloud and cannot be shared
/// </summary>
public class PersonalCloudConnection : Connection
{
    [JsonPropertyName("allowConnectionUsageInGateway")]
    public bool AllowConnectionUsageInGateway { get; set; }
}

/// <summary>
/// A connection that connects through on-premises data gateway
/// </summary>
public class OnPremisesGatewayConnection : Connection
{
    [JsonPropertyName("gatewayId")]
    public string GatewayId { get; set; } = string.Empty;
}

/// <summary>
/// A connection that connects through a personal on-premises data gateway
/// </summary>
public class OnPremisesGatewayPersonalConnection : Connection
{
    [JsonPropertyName("gatewayId")]
    public string GatewayId { get; set; } = string.Empty;
}

/// <summary>
/// A connection that connects through a virtual network data gateway
/// </summary>
public class VirtualNetworkGatewayConnection : Connection
{
    [JsonPropertyName("gatewayId")]
    public string GatewayId { get; set; } = string.Empty;
}