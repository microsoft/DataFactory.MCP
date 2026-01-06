using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Connection.Create;

/// <summary>
/// The credential details input for creating a connection
/// </summary>
public class CreateCredentialDetails
{
    [JsonPropertyName("singleSignOnType")]
    public SingleSignOnType SingleSignOnType { get; set; }

    [JsonPropertyName("connectionEncryption")]
    public ConnectionEncryption ConnectionEncryption { get; set; }

    [JsonPropertyName("skipTestConnection")]
    public bool SkipTestConnection { get; set; }

    [JsonPropertyName("credentials")]
    public Credentials.Credentials? Credentials { get; set; }
}