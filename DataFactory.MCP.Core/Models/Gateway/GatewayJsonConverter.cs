using System.Text.Json;
using System.Text.Json.Serialization;
using DataFactory.MCP.Configuration;

namespace DataFactory.MCP.Models.Gateway;

/// <summary>
/// Custom JSON converter for Gateway polymorphic deserialization.
/// Uses source-generated JsonTypeInfo for trim-safe serialization.
/// </summary>
public class GatewayJsonConverter : JsonConverter<Gateway>
{
    public override Gateway Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;

        if (!root.TryGetProperty("type", out JsonElement typeElement))
        {
            throw new JsonException("Gateway object must have a 'type' property");
        }

        string gatewayType = typeElement.GetString() ?? string.Empty;

        return gatewayType switch
        {
            "OnPremises" => JsonSerializer.Deserialize(root.GetRawText(), DataFactoryJsonContext.Default.OnPremisesGateway)!,
            "OnPremisesPersonal" => JsonSerializer.Deserialize(root.GetRawText(), DataFactoryJsonContext.Default.OnPremisesGatewayPersonal)!,
            "VirtualNetwork" => JsonSerializer.Deserialize(root.GetRawText(), DataFactoryJsonContext.Default.VirtualNetworkGateway)!,
            _ => throw new JsonException($"Unknown gateway type: {gatewayType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, Gateway value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case OnPremisesGateway g:
                JsonSerializer.Serialize(writer, g, DataFactoryJsonContext.Default.OnPremisesGateway);
                break;
            case OnPremisesGatewayPersonal g:
                JsonSerializer.Serialize(writer, g, DataFactoryJsonContext.Default.OnPremisesGatewayPersonal);
                break;
            case VirtualNetworkGateway g:
                JsonSerializer.Serialize(writer, g, DataFactoryJsonContext.Default.VirtualNetworkGateway);
                break;
            default:
                throw new JsonException($"Unsupported gateway type: {value.GetType()}");
        }
    }
}
