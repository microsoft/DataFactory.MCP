using DataFactory.MCP.Models.Connection.Interfaces;
using DataFactory.MCP.Models.Connection.Create.Credentials;

namespace DataFactory.MCP.Models.Connection.Configurations;

/// <summary>
/// Basic authentication configuration
/// </summary>
public class BasicAuthConfig : IAuthConfig
{
    public CredentialType Type => CredentialType.Basic;
    public SingleSignOnType SingleSignOnType => SingleSignOnType.None;
    public ConnectionEncryption ConnectionEncryption { get; }
    public bool SkipTestConnection { get; }
    public string Username { get; }
    public string Password { get; }

    public BasicAuthConfig(string username, string password,
        ConnectionEncryption connectionEncryption = ConnectionEncryption.Encrypted,
        bool skipTestConnection = false)
    {
        Username = username;
        Password = password;
        ConnectionEncryption = connectionEncryption;
        SkipTestConnection = skipTestConnection;
    }

    public Credentials GetCredentials()
    {
        return new BasicCredentials
        {
            Username = Username,
            Password = Password
        };
    }

    public static BasicAuthConfig Create(string username, string password, bool skipTestConnection = false)
    {
        return new BasicAuthConfig(username, password, ConnectionEncryption.Encrypted, skipTestConnection);
    }
}