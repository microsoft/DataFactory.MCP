using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fabric.Mcp.Tools.DataFactory.Models.Gateway;

/// <summary>
/// Custom JSON converter for Gateway polymorphic deserialization
/// </summary>
public class GatewayJsonConverter : JsonConverter<Gateway>
{
    public override Gateway Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;

        // Get the type property to determine which concrete type to deserialize to
        if (!root.TryGetProperty("type", out JsonElement typeElement))
        {
            throw new JsonException("Gateway object must have a 'type' property");
        }

        string gatewayType = typeElement.GetString() ?? string.Empty;

        // Deserialize to the appropriate concrete type based on the type field
#pragma warning disable IL2026, IL3050
        return gatewayType switch
        {
            "OnPremises" => JsonSerializer.Deserialize<OnPremisesGateway>(root.GetRawText(), options)!,
            "OnPremisesPersonal" => JsonSerializer.Deserialize<OnPremisesGatewayPersonal>(root.GetRawText(), options)!,
            "VirtualNetwork" => JsonSerializer.Deserialize<VirtualNetworkGateway>(root.GetRawText(), options)!,
            _ => throw new JsonException($"Unknown gateway type: {gatewayType}")
        };
#pragma warning restore IL2026, IL3050
    }

    public override void Write(Utf8JsonWriter writer, Gateway value, JsonSerializerOptions options)
    {
#pragma warning disable IL2026, IL3050
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
#pragma warning restore IL2026, IL3050
    }
}
