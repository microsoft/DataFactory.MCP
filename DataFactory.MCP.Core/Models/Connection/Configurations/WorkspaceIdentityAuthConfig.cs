using DataFactory.MCP.Models.Connection.Interfaces;
using DataFactory.MCP.Models.Connection.Create.Credentials;

namespace DataFactory.MCP.Models.Connection.Configurations;

/// <summary>
/// Workspace identity authentication configuration
/// </summary>
public class WorkspaceIdentityAuthConfig : IAuthConfig
{
    public CredentialType Type => CredentialType.WorkspaceIdentity;
    public SingleSignOnType SingleSignOnType => SingleSignOnType.None;
    public ConnectionEncryption ConnectionEncryption { get; }
    public bool SkipTestConnection { get; }

    public WorkspaceIdentityAuthConfig(ConnectionEncryption connectionEncryption = ConnectionEncryption.Encrypted,
        bool skipTestConnection = false)
    {
        ConnectionEncryption = connectionEncryption;
        SkipTestConnection = skipTestConnection;
    }

    public Credentials GetCredentials()
    {
        return new WorkspaceIdentityCredentials();
    }

    public static WorkspaceIdentityAuthConfig Create(bool skipTestConnection = false)
    {
        return new WorkspaceIdentityAuthConfig(ConnectionEncryption.Encrypted, skipTestConnection);
    }
}