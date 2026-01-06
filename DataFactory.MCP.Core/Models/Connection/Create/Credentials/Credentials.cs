using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Connection.Create.Credentials;

/// <summary>
/// Base class for credentials
/// </summary>
public abstract class Credentials
{
    [JsonPropertyName("credentialType")]
    public CredentialType CredentialType { get; set; }
}