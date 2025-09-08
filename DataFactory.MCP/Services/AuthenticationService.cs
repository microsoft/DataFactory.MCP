using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace DataFactory.MCP.Services;

/// <summary>
/// Azure AD authentication service implementation for DataFactory MCP
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly AzureAdConfiguration _azureAdConfig;
    private McpAuthenticationResult? _currentAuth;
    private IPublicClientApplication? _publicClientApp;

    public AuthenticationService(
        ILogger<AuthenticationService> logger,
        IOptions<AzureAdConfiguration> azureAdConfig)
    {
        _logger = logger;
        _azureAdConfig = azureAdConfig.Value;
        InitializeClientApplications();
    }

    private void InitializeClientApplications()
    {
        try
        {
            // Initialize public client for interactive authentication
            _publicClientApp = PublicClientApplicationBuilder
                .Create(_azureAdConfig.ClientId)
                .WithAuthority(_azureAdConfig.Authority)
                .WithRedirectUri(_azureAdConfig.RedirectUri)
                .Build();

            _logger.LogInformation("Azure AD client applications initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure AD client applications");
            throw;
        }
    }

    public async Task<string> AuthenticateInteractiveAsync()
    {
        try
        {
            if (_publicClientApp == null)
            {
                return "Public client application not initialized. Check Azure AD configuration.";
            }

            _logger.LogInformation("Starting interactive authentication...");

            var result = await _publicClientApp
                .AcquireTokenInteractive(_azureAdConfig.DefaultScopes)
                .ExecuteAsync();

            _currentAuth = McpAuthenticationResult.Success(
                result.AccessToken,
                result.Account.Username,
                result.TenantId,
                result.ExpiresOn.DateTime,
                string.Join(", ", result.Scopes)
            );

            _logger.LogInformation("Interactive authentication completed successfully for user: {Username}", result.Account.Username);
            return $"Interactive authentication completed successfully. User: {result.Account.Username}";
        }
        catch (MsalException msalEx)
        {
            _logger.LogError(msalEx, "MSAL authentication failed");
            _currentAuth = null;
            return $"Authentication failed: {msalEx.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Interactive authentication failed");
            _currentAuth = null;
            return $"Authentication error: {ex.Message}";
        }
    }

    public async Task<string> AuthenticateServicePrincipalAsync(string applicationId, string clientSecret, string? tenantId = null)
    {
        try
        {
            _logger.LogInformation("Starting service principal authentication for app: {ApplicationId}", applicationId);

            // Create a confidential client application for this specific authentication
            var authority = $"https://login.microsoftonline.com/{tenantId ?? _azureAdConfig.TenantId}";
            var confidentialClient = ConfidentialClientApplicationBuilder
                .Create(applicationId)
                .WithClientSecret(clientSecret)
                .WithAuthority(authority)
                .Build();

            var result = await confidentialClient
                .AcquireTokenForClient(_azureAdConfig.DefaultScopes)
                .ExecuteAsync();

            _currentAuth = McpAuthenticationResult.Success(
                result.AccessToken,
                $"ServicePrincipal-{applicationId}",
                tenantId ?? _azureAdConfig.TenantId,
                result.ExpiresOn.DateTime,
                string.Join(", ", result.Scopes)
            );

            _logger.LogInformation("Service principal authentication completed successfully for app: {ApplicationId}", applicationId);
            return $"Service principal authentication completed successfully for application: {applicationId}";
        }
        catch (MsalException msalEx)
        {
            _logger.LogError(msalEx, "MSAL service principal authentication failed for app: {ApplicationId}", applicationId);
            _currentAuth = null;
            return $"Service principal authentication failed: {msalEx.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service principal authentication failed for app: {ApplicationId}", applicationId);
            _currentAuth = null;
            return $"Authentication error: {ex.Message}";
        }
    }

    public string GetAuthenticationStatus()
    {
        if (_currentAuth == null || !_currentAuth.IsSuccess)
        {
            return "Not authenticated. Please authenticate using interactive login or service principal.";
        }

        var status = $"""
            Authentication Status: Authenticated
            User: {_currentAuth.UserName}
            Tenant: {_currentAuth.TenantId}
            Token Expires: {_currentAuth.ExpiresOn:yyyy-MM-dd HH:mm:ss} UTC
            Scopes: {_currentAuth.Scopes}
            """;

        return status;
    }

    public async Task<string> SignOutAsync()
    {
        try
        {
            _logger.LogInformation("Signing out current user...");

            if (_currentAuth == null)
            {
                return "No active authentication session found.";
            }

            var userName = _currentAuth.UserName;

            // Clear cached tokens if using interactive authentication
            if (_publicClientApp != null && !userName?.StartsWith("ServicePrincipal-") == true)
            {
                var accounts = await _publicClientApp.GetAccountsAsync();
                foreach (var account in accounts)
                {
                    await _publicClientApp.RemoveAsync(account);
                }
            }

            _currentAuth = null;
            _logger.LogInformation("Successfully signed out user: {UserName}", userName);
            return $"Successfully signed out user: {userName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sign out");
            return $"Sign out error: {ex.Message}";
        }
    }

    public async Task<string> GetAccessTokenAsync()
    {
        try
        {
            if (_currentAuth == null || !_currentAuth.IsSuccess)
            {
                return "No valid authentication found. Please authenticate first.";
            }

            if (_currentAuth.ExpiresOn.HasValue && _currentAuth.ExpiresOn <= DateTime.UtcNow)
            {
                // Try to refresh the token silently
                if (_publicClientApp != null && !_currentAuth.UserName?.StartsWith("ServicePrincipal-") == true)
                {
                    var accounts = await _publicClientApp.GetAccountsAsync();
                    if (accounts.Any())
                    {
                        try
                        {
                            var result = await _publicClientApp
                                .AcquireTokenSilent(_azureAdConfig.DefaultScopes, accounts.First())
                                .ExecuteAsync();

                            _currentAuth = McpAuthenticationResult.Success(
                                result.AccessToken,
                                result.Account.Username,
                                result.TenantId,
                                result.ExpiresOn.DateTime,
                                string.Join(", ", result.Scopes)
                            );

                            return _currentAuth.AccessToken ?? "Token not available";
                        }
                        catch (MsalUiRequiredException)
                        {
                            return "Access token has expired and cannot be refreshed silently. Please re-authenticate.";
                        }
                    }
                }

                return "Access token has expired. Please re-authenticate.";
            }

            return _currentAuth.AccessToken ?? "Token not available";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving access token");
            return $"Error retrieving access token: {ex.Message}";
        }
    }
}
