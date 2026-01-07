using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace DataFactory.MCP.Services;

/// <summary>
/// Azure AD authentication service implementation for DataFactory MCP
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly ITokenAccessor _tokenAccessor;
    private McpAuthenticationResult? _currentAuth;
    private IPublicClientApplication? _publicClientApp;

    public AuthenticationService(ILogger<AuthenticationService> logger, ITokenAccessor tokenAccessor)
    {
        _logger = logger;
        _tokenAccessor = tokenAccessor;
        InitializeClientApplications();
    }

    private void InitializeClientApplications()
    {
        try
        {
            // Initialize public client for interactive authentication
            _publicClientApp = PublicClientApplicationBuilder
            .Create(AzureAdConfiguration.ClientId)
            .WithAuthority(new Uri(AzureAdConfiguration.Authority))
            .WithRedirectUri(AzureAdConfiguration.RedirectUri)
            .Build();

            _logger.LogInformation(Messages.AzureAdClientInitializedSuccessfully);
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
                return Messages.PublicClientNotInitialized;
            }

            _logger.LogInformation(Messages.StartingInteractiveAuthentication);

            var result = await _publicClientApp
                .AcquireTokenInteractive(AzureAdConfiguration.PowerBIScopes)
                .ExecuteAsync();

            _currentAuth = McpAuthenticationResult.Success(
                result.AccessToken,
                result.Account.Username,
                result.TenantId,
                result.ExpiresOn.DateTime,
                string.Join(", ", result.Scopes)
            );

            _logger.LogInformation("Interactive authentication completed successfully for user: {Username}", result.Account.Username);
            return string.Format(Messages.InteractiveAuthenticationSuccessTemplate, result.Account.Username);
        }
        catch (MsalException msalEx)
        {
            _logger.LogError(msalEx, "MSAL authentication failed");
            _currentAuth = null;
            return string.Format(Messages.AuthenticationFailedTemplate, msalEx.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Interactive authentication failed");
            _currentAuth = null;
            return string.Format(Messages.AuthenticationErrorTemplate, ex.Message);
        }
    }

    public string AuthenticateWithExternalToken(string accessToken, string? userName = null)
    {
        try
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return "External token cannot be null or empty.";
            }

            _logger.LogInformation("Setting external access token for user: {UserName}", userName ?? "Unknown");

            // Store the token in the token accessor for per-request access
            _tokenAccessor.SetAccessToken(accessToken);

            // Also update current auth state for status checks
            _currentAuth = McpAuthenticationResult.Success(
                accessToken,
                userName ?? "ExternalToken",
                null,
                null, // We don't know when external tokens expire
                "External Token (passthrough)"
            );

            return $"Successfully authenticated with external token for: {userName ?? "Unknown user"}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set external token");
            return string.Format(Messages.AuthenticationErrorTemplate, ex.Message);
        }
    }

    public async Task<string> AuthenticateServicePrincipalAsync(string applicationId, string clientSecret, string? tenantId = null)
    {
        try
        {
            _logger.LogInformation("Starting service principal authentication for app: {ApplicationId}", applicationId);

            // Create a confidential client application for this specific authentication
            var authority = $"https://login.microsoftonline.com/{tenantId ?? AzureAdConfiguration.TenantId}";
            var confidentialClient = ConfidentialClientApplicationBuilder
                .Create(applicationId)
                .WithClientSecret(clientSecret)
                .WithAuthority(authority)
                .Build();

            var result = await confidentialClient
                .AcquireTokenForClient(AzureAdConfiguration.PowerBIScopes)
                .ExecuteAsync();

            _currentAuth = McpAuthenticationResult.Success(
                result.AccessToken,
                $"ServicePrincipal-{applicationId}",
                tenantId ?? AzureAdConfiguration.TenantId,
                result.ExpiresOn.DateTime,
                string.Join(", ", result.Scopes)
            );

            _logger.LogInformation("Service principal authentication completed successfully for app: {ApplicationId}", applicationId);
            return string.Format(Messages.ServicePrincipalAuthenticationSuccessTemplate, applicationId);
        }
        catch (MsalException msalEx)
        {
            _logger.LogError(msalEx, "MSAL service principal authentication failed for app: {ApplicationId}", applicationId);
            _currentAuth = null;
            return string.Format(Messages.ServicePrincipalAuthenticationFailedTemplate, msalEx.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service principal authentication failed for app: {ApplicationId}", applicationId);
            _currentAuth = null;
            return string.Format(Messages.AuthenticationErrorTemplate, ex.Message);
        }
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

            _currentAuth = null;
            _logger.LogInformation("Successfully signed out user: {UserName}", userName);
            return string.Format(Messages.SignOutSuccessTemplate, userName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sign out");
            return string.Format(Messages.SignOutErrorTemplate, ex.Message);
        }
    }

    public async Task<string> GetAccessTokenAsync()
    {
        try
        {
            // First, check if we have an external token (OAuth passthrough)
            if (_tokenAccessor.HasExternalToken)
            {
                _logger.LogDebug("Using external token from token accessor");
                return _tokenAccessor.AccessToken!;
            }

            if (_currentAuth == null || !_currentAuth.IsSuccess)
            {
                return Messages.NoAuthenticationFound;
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
                                .AcquireTokenSilent(AzureAdConfiguration.PowerBIScopes, accounts.First())
                                .ExecuteAsync();

                            _currentAuth = McpAuthenticationResult.Success(
                                result.AccessToken,
                                result.Account.Username,
                                result.TenantId,
                                result.ExpiresOn.DateTime,
                                string.Join(", ", result.Scopes)
                            );

                            return _currentAuth.AccessToken ?? Messages.TokenNotAvailable;
                        }
                        catch (MsalUiRequiredException)
                        {
                            return Messages.AccessTokenExpiredCannotRefresh;
                        }
                    }
                }

                return Messages.AccessTokenExpired;
            }

            return _currentAuth.AccessToken ?? Messages.TokenNotAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving access token");
            return string.Format(Messages.ErrorRetrievingAccessTokenTemplate, ex.Message);
        }
    }

    public async Task<string> GetAccessTokenAsync(string[] scopes)
    {
        try
        {
            // First, check if we have an external token (OAuth passthrough)
            // Note: With external tokens, we can't request different scopes - we use what was given
            if (_tokenAccessor.HasExternalToken)
            {
                _logger.LogDebug("Using external token from token accessor (requested scopes: {Scopes})", string.Join(", ", scopes));
                return _tokenAccessor.AccessToken!;
            }

            if (_publicClientApp == null)
            {
                return Messages.PublicClientNotInitialized;
            }

            _logger.LogInformation("Acquiring token for scopes: {Scopes}", string.Join(", ", scopes));

            // Try to get token silently first if we have an authenticated account
            var accounts = await _publicClientApp.GetAccountsAsync();
            if (accounts.Any())
            {
                try
                {
                    var result = await _publicClientApp
                        .AcquireTokenSilent(scopes, accounts.First())
                        .ExecuteAsync();

                    _logger.LogInformation("Token acquired silently for scopes: {Scopes}", string.Join(", ", result.Scopes));
                    return result.AccessToken;
                }
                catch (MsalUiRequiredException)
                {
                    _logger.LogInformation("Silent token acquisition failed, falling back to interactive");
                }
            }

            // If silent acquisition fails, try interactive
            var interactiveResult = await _publicClientApp
                .AcquireTokenInteractive(scopes)
                .ExecuteAsync();

            _logger.LogInformation("Token acquired interactively for scopes: {Scopes}", string.Join(", ", interactiveResult.Scopes));
            return interactiveResult.AccessToken;
        }
        catch (MsalException msalEx)
        {
            _logger.LogError(msalEx, "MSAL token acquisition failed for scopes: {Scopes}", string.Join(", ", scopes));
            return string.Format(Messages.AuthenticationFailedTemplate, msalEx.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token acquisition failed for scopes: {Scopes}", string.Join(", ", scopes));
            return string.Format(Messages.ErrorRetrievingAccessTokenTemplate, ex.Message);
        }
    }
}
