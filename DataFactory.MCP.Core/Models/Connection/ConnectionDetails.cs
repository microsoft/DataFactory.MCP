using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Connection;

/// <summary>
/// The connection details output for list operations
/// </summary>
public class ListConnectionDetails
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
}

/// <summary>
/// The credential details returned when fetching a connection
/// </summary>
public class ListCredentialDetails
{
    [JsonPropertyName("credentialType")]
    public CredentialType CredentialType { get; set; }

    [JsonPropertyName("singleSignOnType")]
    public SingleSignOnType SingleSignOnType { get; set; }

    [JsonPropertyName("connectionEncryption")]
    public ConnectionEncryption ConnectionEncryption { get; set; }

    [JsonPropertyName("skipTestConnection")]
    public bool SkipTestConnection { get; set; }
}