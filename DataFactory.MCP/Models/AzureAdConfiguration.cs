namespace DataFactory.MCP.Models;

/// <summary>
/// Configuration settings for Azure Active Directory authentication
/// </summary>
public class AzureAdConfiguration
{
    public const string SectionName = "AzureAd";

    /// <summary>
    /// Azure AD tenant ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Application (client) ID from Azure AD app registration
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret (for service principal authentication)
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Azure AD authority URL
    /// </summary>
    public string Authority => $"https://login.microsoftonline.com/{TenantId}";

    /// <summary>
    /// Default scopes for authentication
    /// </summary>
    public string[] DefaultScopes { get; set; } = new[] { "https://analysis.windows.net/powerbi/api/.default" };

    /// <summary>
    /// Redirect URI for interactive authentication
    /// </summary>
    public string RedirectUri { get; set; } = "http://localhost:8080";
}
