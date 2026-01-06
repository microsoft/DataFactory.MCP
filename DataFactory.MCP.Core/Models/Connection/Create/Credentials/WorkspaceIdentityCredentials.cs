namespace DataFactory.MCP.Models.Connection.Create.Credentials;

/// <summary>
/// Credentials for WorkspaceIdentity CredentialType
/// </summary>
public class WorkspaceIdentityCredentials : Credentials
{
    public WorkspaceIdentityCredentials()
    {
        CredentialType = CredentialType.WorkspaceIdentity;
    }
}