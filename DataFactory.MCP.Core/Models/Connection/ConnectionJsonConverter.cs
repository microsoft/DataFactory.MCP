using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Connection;

/// <summary>
/// JSON converter for handling polymorphic Connection types based on ConnectivityType
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
        var jsonOptions = new JsonSerializerOptions(options);
        jsonOptions.Converters.Remove(this); // Prevent infinite recursion

        return connectivityType switch
        {
            ConnectivityType.ShareableCloud => JsonSerializer.Deserialize<ShareableCloudConnection>(json, jsonOptions),
            ConnectivityType.PersonalCloud => JsonSerializer.Deserialize<PersonalCloudConnection>(json, jsonOptions),
            ConnectivityType.OnPremisesGateway => JsonSerializer.Deserialize<OnPremisesGatewayConnection>(json, jsonOptions),
            ConnectivityType.OnPremisesGatewayPersonal => JsonSerializer.Deserialize<OnPremisesGatewayPersonalConnection>(json, jsonOptions),
            ConnectivityType.VirtualNetworkGateway => JsonSerializer.Deserialize<VirtualNetworkGatewayConnection>(json, jsonOptions),
            _ => throw new JsonException($"Unsupported connectivity type: {connectivityType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, Connection value, JsonSerializerOptions options)
    {
        var jsonOptions = new JsonSerializerOptions(options);
        jsonOptions.Converters.Remove(this); // Prevent infinite recursion
        JsonSerializer.Serialize(writer, value, value.GetType(), jsonOptions);
    }
}