namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Provides authentication services for the MCP server
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticate with Azure AD using interactive login
    /// </summary>
    Task<string> AuthenticateInteractiveAsync();

    /// <summary>
    /// Start device code authentication - returns device code and URL immediately
    /// </summary>
    Task<string> StartDeviceCodeAuthAsync();

    /// <summary>
    /// Check the status of pending device code authentication
    /// </summary>
    Task<string> CheckDeviceAuthStatusAsync();

    /// <summary>
    /// Authenticate with Azure AD using service principal and client secret
    /// </summary>
    Task<string> AuthenticateServicePrincipalAsync(string applicationId, string clientSecret, string? tenantId = null);

    /// <summary>
    /// Get current authentication status and profile information
    /// </summary>
    string GetAuthenticationStatus();

    /// <summary>
    /// Clear current authentication and sign out
    /// </summary>
    Task<string> SignOutAsync();

    /// <summary>
    /// Get current access token for authenticated user
    /// </summary>
    Task<string> GetAccessTokenAsync();

    /// <summary>
    /// Get access token for specific scopes
    /// </summary>
    Task<string> GetAccessTokenAsync(string[] scopes);
}
