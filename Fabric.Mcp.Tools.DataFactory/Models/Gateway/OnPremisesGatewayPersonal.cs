using System.Text.Json.Serialization;

namespace Fabric.Mcp.Tools.DataFactory.Models.Gateway;

/// <summary>
/// On-premises gateway (personal mode)
/// </summary>
public class OnPremisesGatewayPersonal : Gateway
{
    /// <summary>
    /// The public key of the gateway
    /// </summary>
    [JsonPropertyName("publicKey")]
    public PublicKey PublicKey { get; set; } = new();

    /// <summary>
    /// The version of the gateway
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}
