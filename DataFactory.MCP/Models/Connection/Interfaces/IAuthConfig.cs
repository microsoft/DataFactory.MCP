namespace DataFactory.MCP.Models.Connection.Interfaces;

/// <summary>
/// Interface for authentication configuration
/// </summary>
public interface IAuthConfig
{
    CredentialType Type { get; }
    SingleSignOnType SingleSignOnType { get; }
    ConnectionEncryption ConnectionEncryption { get; }
    bool SkipTestConnection { get; }
    Credentials GetCredentials();
}