using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Configuration;
using DataFactory.MCP.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace DataFactory.MCP.Services;

/// <summary>
/// Manages authentication state, tokens, and provides access token operations
/// </summary>
public class AuthenticationStateManager : IAuthenticationStateManager
{
    private readonly ILogger<AuthenticationStateManager> _logger;
    private McpAuthenticationResult? _currentAuth;
    private IPublicClientApplication? _publicClientApp;

    public AuthenticationStateManager(ILogger<AuthenticationStateManager> logger)
    {
        _logger = logger;
        InitializePublicClient();
    }

    public McpAuthenticationResult? CurrentAuthentication => _currentAuth;

    public void SetAuthentication(McpAuthenticationResult result)
    {
        _currentAuth = result;
        _logger.LogInformation("Authentication state updated for user: {UserName}", result.UserName);
    }

    public void ClearAuthentication()
    {
        var userName = _currentAuth?.UserName;
        _currentAuth = null;
        _logger.LogInformation("Authentication state cleared for user: {UserName}", userName);
    }

    public string GetAuthenticationStatus()
    {
        if (_currentAuth == null || !_currentAuth.IsSuccess)
        {
            return Messages.NotAuthenticated;
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

    public async Task<string> GetAccessTokenAsync(string[]? scopes = null)
    {
        try
        {
            if (_currentAuth == null || !_currentAuth.IsSuccess)
            {
                return Messages.NoAuthenticationFound;
            }

            var targetScopes = scopes ?? AzureAdConfiguration.PowerBIScopes;

            // Check if current token is expired
            if (_currentAuth.ExpiresOn.HasValue && _currentAuth.ExpiresOn <= DateTime.UtcNow)
            {
                // Try to refresh the token silently for non-service principal accounts
                if (_publicClientApp != null && !_currentAuth.UserName?.StartsWith("ServicePrincipal-") == true)
                {
                    var refreshResult = await TryRefreshTokenAsync(targetScopes);
                    if (!string.IsNullOrEmpty(refreshResult))
                    {
                        return refreshResult;
                    }
                }
                else
                {
                    return Messages.AccessTokenExpired;
                }
            }

            return _currentAuth.AccessToken ?? Messages.TokenNotAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving access token");
            return string.Format(Messages.ErrorRetrievingAccessTokenTemplate, ex.Message);
        }
    }

    public async Task<string> SignOutAsync()
    {
        try
        {
            _logger.LogInformation(Messages.SigningOutCurrentUser);

            if (_currentAuth == null)
            {
                return Messages.NoActiveAuthenticationSession;
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

            ClearAuthentication();
            _logger.LogInformation("Successfully signed out user: {UserName}", userName);
            return string.Format(Messages.SignOutSuccessTemplate, userName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sign out");
            return string.Format(Messages.SignOutErrorTemplate, ex.Message);
        }
    }

    private void InitializePublicClient()
    {
        try
        {
            _publicClientApp = PublicClientApplicationBuilder
                .Create(AzureAdConfiguration.ClientId)
                .WithAuthority(new Uri(AzureAdConfiguration.Authority))
                .WithRedirectUri(AzureAdConfiguration.RedirectUri)
                .Build();

            _logger.LogInformation("Public client application initialized for token refresh");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize public client application");
        }
    }

    private async Task<string?> TryRefreshTokenAsync(string[] scopes)
    {
        try
        {
            if (_publicClientApp == null)
                return null;

            var accounts = await _publicClientApp.GetAccountsAsync();
            if (accounts.Any())
            {
                var result = await _publicClientApp
                    .AcquireTokenSilent(scopes, accounts.First())
                    .ExecuteAsync();

                _currentAuth = McpAuthenticationResult.Success(
                    result.AccessToken,
                    result.Account.Username,
                    result.TenantId,
                    result.ExpiresOn.DateTime,
                    string.Join(", ", result.Scopes)
                );

                _logger.LogInformation("Token refreshed successfully for user: {UserName}", result.Account.Username);
                return result.AccessToken;
            }
        }
        catch (MsalUiRequiredException)
        {
            _logger.LogInformation("Silent token refresh failed, UI interaction required");
            return Messages.AccessTokenExpiredCannotRefresh;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
        }

        return null;
    }
}