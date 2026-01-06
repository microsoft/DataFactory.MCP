using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Connection.Create.Credentials;

/// <summary>
/// Credentials for Basic CredentialType
/// </summary>
public class BasicCredentials : Credentials
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    public BasicCredentials()
    {
        CredentialType = CredentialType.Basic;
    }
}