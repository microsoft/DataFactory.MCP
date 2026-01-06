namespace DataFactory.MCP.Models.Connection.Create.Credentials;

/// <summary>
/// Credentials for OAuth2 CredentialType
/// </summary>
public class OAuth2Credentials : Credentials
{
    public OAuth2Credentials()
    {
        CredentialType = CredentialType.OAuth2;
    }
}