using System.Text.Json;
using System.Text.Json.Serialization;
using DataFactory.MCP.Models.Connection.Create.Credentials;

namespace DataFactory.MCP.Models.Connection;

/// <summary>
/// JSON converter for Credentials polymorphic types
/// </summary>
public class CredentialsJsonConverter : JsonConverter<Credentials>
{
    public override Credentials? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument document = JsonDocument.ParseValue(ref reader))
        {
            var root = document.RootElement;

            if (root.TryGetProperty("credentialType", out var credentialTypeProperty))
            {
                var credentialType = credentialTypeProperty.GetString();

                return credentialType switch
                {
                    "Basic" => JsonSerializer.Deserialize<BasicCredentials>(root.GetRawText(), options),
                    "Anonymous" => JsonSerializer.Deserialize<AnonymousCredentials>(root.GetRawText(), options),
                    "OAuth2" => JsonSerializer.Deserialize<OAuth2Credentials>(root.GetRawText(), options),
                    "WorkspaceIdentity" => JsonSerializer.Deserialize<WorkspaceIdentityCredentials>(root.GetRawText(), options),
                    _ => throw new JsonException($"Unknown credential type: {credentialType}")
                };
            }

            throw new JsonException("Missing credentialType property");
        }
    }

    public override void Write(Utf8JsonWriter writer, Credentials value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}