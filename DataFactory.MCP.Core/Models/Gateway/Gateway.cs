using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Gateway;

/// <summary>
/// Base gateway class
/// </summary>
[JsonConverter(typeof(GatewayJsonConverter))]
public abstract class Gateway
{
    /// <summary>
    /// The object ID of the gateway
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The type of the gateway
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}
