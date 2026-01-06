using System.Text.Json;
using System.Text.Json.Serialization;
using DataFactory.MCP.Models.Connection.Create.Parameters;

namespace DataFactory.MCP.Models.Connection;

/// <summary>
/// JSON converter for ConnectionDetailsParameter polymorphic types
/// </summary>
public class ConnectionDetailsParameterJsonConverter : JsonConverter<ConnectionDetailsParameter>
{
    public override ConnectionDetailsParameter? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument document = JsonDocument.ParseValue(ref reader))
        {
            var root = document.RootElement;

            if (root.TryGetProperty("dataType", out var dataTypeProperty))
            {
                var dataType = dataTypeProperty.GetString();

                return dataType switch
                {
                    "Text" => JsonSerializer.Deserialize<ConnectionDetailsTextParameter>(root.GetRawText(), options),
                    "Number" => JsonSerializer.Deserialize<ConnectionDetailsNumberParameter>(root.GetRawText(), options),
                    "Boolean" => JsonSerializer.Deserialize<ConnectionDetailsBooleanParameter>(root.GetRawText(), options),
                    _ => throw new JsonException($"Unknown data type: {dataType}")
                };
            }

            throw new JsonException("Missing dataType property");
        }
    }

    public override void Write(Utf8JsonWriter writer, ConnectionDetailsParameter value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}