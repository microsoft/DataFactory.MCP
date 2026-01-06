namespace DataFactory.MCP.Models.Connection.Create.Credentials;

/// <summary>
/// Credentials for Anonymous CredentialType
/// </summary>
public class AnonymousCredentials : Credentials
{
    public AnonymousCredentials()
    {
        CredentialType = CredentialType.Anonymous;
    }
}