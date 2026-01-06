using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Connection.Create;

/// <summary>
/// Base request for creating connections
/// </summary>
public abstract class CreateConnectionRequest
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("connectivityType")]
    public ConnectivityType ConnectivityType { get; set; }

    [JsonPropertyName("connectionDetails")]
    public CreateConnectionDetails ConnectionDetails { get; set; } = new();

    [JsonPropertyName("privacyLevel")]
    public PrivacyLevel? PrivacyLevel { get; set; }

    [JsonPropertyName("credentialDetails")]
    public CreateCredentialDetails CredentialDetails { get; set; } = new();
}