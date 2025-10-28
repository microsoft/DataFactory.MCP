namespace DataFactory.MCP.Models;

/// <summary>
/// Configuration settings for Azure Active Directory authentication
/// </summary>
public static class AzureAdConfiguration
{
    /// <summary>
    /// Azure AD tenant ID
    /// </summary>
    public const string TenantId = "common";

    /// <summary>
    /// Application (client) ID from Azure AD app registration
    /// Azure CLI public client ID - https://github.com/Azure/azure-cli/blob/main/src/azure-cli-core/azure/cli/core/auth/constants.py
    /// </summary>
    public const string ClientId = "04b07795-8ddb-461a-bbee-02f9e1bf7b46";

    /// <summary>
    /// Azure AD authority URL
    /// </summary>
    public const string Authority = "https://login.microsoftonline.com/organizations";

    /// <summary>
    /// Power BI specific scopes
    /// </summary>
    public static readonly string[] PowerBIScopes = new[] { "https://analysis.windows.net/powerbi/api/.default" };

    /// <summary>
    /// Azure Resource Manager specific scopes
    /// </summary>
    public static readonly string[] AzureResourceManagerScopes = new[] { "https://management.azure.com/.default" };

    /// <summary>
    /// Redirect URI for interactive authentication
    /// </summary>
    public const string RedirectUri = "http://localhost:0";
}
