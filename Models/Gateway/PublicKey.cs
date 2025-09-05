using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Gateway;

/// <summary>
/// The public key of the on-premises gateway
/// </summary>
public class PublicKey
{
    /// <summary>
    /// The exponent of the public key
    /// </summary>
    [JsonPropertyName("exponent")]
    public string Exponent { get; set; } = string.Empty;

    /// <summary>
    /// The modulus of the public key
    /// </summary>
    [JsonPropertyName("modulus")]
    public string Modulus { get; set; } = string.Empty;
}
