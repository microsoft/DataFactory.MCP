using System.Text.Json;
using System.Text.Json.Serialization;
using DataFactory.MCP.Configuration;

namespace DataFactory.MCP.Models.Connection;

/// <summary>
/// JSON converter for handling polymorphic Connection types based on ConnectivityType.
/// Uses source-generated JsonTypeInfo for trim-safe serialization.
/// </summary>
public class ConnectionJsonConverter : JsonConverter<Connection>
{
    public override Connection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("connectivityType", out var connectivityTypeElement))
        {
            throw new JsonException("Missing connectivityType property");
        }

        var connectivityTypeStr = connectivityTypeElement.GetString();
        if (!Enum.TryParse<ConnectivityType>(connectivityTypeStr, out var connectivityType))
        {
            throw new JsonException($"Unknown connectivity type: {connectivityTypeStr}");
        }

        var json = root.GetRawText();

        return connectivityType switch
        {
            ConnectivityType.ShareableCloud => JsonSerializer.Deserialize(json, DataFactoryJsonContext.Default.ShareableCloudConnection),
            ConnectivityType.PersonalCloud => JsonSerializer.Deserialize(json, DataFactoryJsonContext.Default.PersonalCloudConnection),
            ConnectivityType.OnPremisesGateway => JsonSerializer.Deserialize(json, DataFactoryJsonContext.Default.OnPremisesGatewayConnection),
            ConnectivityType.OnPremisesGatewayPersonal => JsonSerializer.Deserialize(json, DataFactoryJsonContext.Default.OnPremisesGatewayPersonalConnection),
            ConnectivityType.VirtualNetworkGateway => JsonSerializer.Deserialize(json, DataFactoryJsonContext.Default.VirtualNetworkGatewayConnection),
            _ => throw new JsonException($"Unsupported connectivity type: {connectivityType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, Connection value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case ShareableCloudConnection c:
                JsonSerializer.Serialize(writer, c, DataFactoryJsonContext.Default.ShareableCloudConnection);
                break;
            case PersonalCloudConnection c:
                JsonSerializer.Serialize(writer, c, DataFactoryJsonContext.Default.PersonalCloudConnection);
                break;
            case OnPremisesGatewayConnection c:
                JsonSerializer.Serialize(writer, c, DataFactoryJsonContext.Default.OnPremisesGatewayConnection);
                break;
            case OnPremisesGatewayPersonalConnection c:
                JsonSerializer.Serialize(writer, c, DataFactoryJsonContext.Default.OnPremisesGatewayPersonalConnection);
                break;
            case VirtualNetworkGatewayConnection c:
                JsonSerializer.Serialize(writer, c, DataFactoryJsonContext.Default.VirtualNetworkGatewayConnection);
                break;
            default:
                throw new JsonException($"Unsupported connection type: {value.GetType()}");
        }
    }
}