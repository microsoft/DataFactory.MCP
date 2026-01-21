using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Services;

/// <summary>
/// Azure AD authentication service orchestrator that delegates to specific authentication providers
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IAuthenticationStateManager _stateManager;
    private readonly IEnumerable<IAuthenticationProvider> _providers;

    public AuthenticationService(
        ILogger<AuthenticationService> logger,
        IAuthenticationStateManager stateManager,
        IEnumerable<IAuthenticationProvider> providers)
    {
        _logger = logger;
        _stateManager = stateManager;
        _providers = providers;
    }

    /// <summary>
    /// Authenticate using interactive browser flow
    /// </summary>
    public async Task<string> AuthenticateInteractiveAsync()
    {
        var request = new InteractiveAuthenticationRequest();
        var result = await ExecuteAuthenticationAsync(request);

        if (result.IsSuccess && result.AuthenticationDetails != null)
        {
            _stateManager.SetAuthentication(result.AuthenticationDetails);
        }

        return result.Message;
    }

    /// <summary>
    /// Start device code authentication - returns device code and URL immediately
    /// </summary>
    public async Task<string> StartDeviceCodeAuthAsync()
    {
        var request = new StartDeviceCodeAuthenticationRequest();
        var result = await ExecuteAuthenticationAsync(request);
        return result.Message;
    }

    /// <summary>
    /// Check the status of pending device code authentication
    /// </summary>
    public async Task<string> CheckDeviceAuthStatusAsync()
    {
        var request = new CheckDeviceCodeAuthenticationRequest();
        var result = await ExecuteAuthenticationAsync(request);

        if (result.IsSuccess && result.AuthenticationDetails != null)
        {
            _stateManager.SetAuthentication(result.AuthenticationDetails);
        }

        return result.Message;
    }

    /// <summary>
    /// Authenticate with Azure AD using service principal and client secret
    /// </summary>
    public async Task<string> AuthenticateServicePrincipalAsync(string applicationId, string clientSecret, string? tenantId = null)
    {
        var request = new ServicePrincipalAuthenticationRequest
        {
            ApplicationId = applicationId,
            ClientSecret = clientSecret,
            TenantId = tenantId
        };

        var result = await ExecuteAuthenticationAsync(request);

        if (result.IsSuccess && result.AuthenticationDetails != null)
        {
            _stateManager.SetAuthentication(result.AuthenticationDetails);
        }

        return result.Message;
    }

    /// <summary>
    /// Get current authentication status and profile information
    /// </summary>
    public string GetAuthenticationStatus()
    {
        return _stateManager.GetAuthenticationStatus();
    }

    /// <summary>
    /// Clear current authentication and sign out
    /// </summary>
    public async Task<string> SignOutAsync()
    {
        return await _stateManager.SignOutAsync();
    }

    /// <summary>
    /// Get current access token for authenticated user
    /// </summary>
    public async Task<string> GetAccessTokenAsync()
    {
        return await _stateManager.GetAccessTokenAsync();
    }

    /// <summary>
    /// Get access token for specific scopes
    /// </summary>
    public async Task<string> GetAccessTokenAsync(string[] scopes)
    {
        return await _stateManager.GetAccessTokenAsync(scopes);
    }

    private async Task<Abstractions.Interfaces.AuthenticationResult> ExecuteAuthenticationAsync(AuthenticationRequest request)
    {
        try
        {
            var provider = _providers.FirstOrDefault(p => p.CanHandle(request));
            if (provider == null)
            {
                _logger.LogError("No authentication provider found for request type: {RequestType}", request.AuthenticationType);
                return Abstractions.Interfaces.AuthenticationResult.Failure($"No provider available for authentication type: {request.AuthenticationType}");
            }

            _logger.LogInformation("Executing authentication using provider: {ProviderType}", provider.ProviderType);
            return await provider.AuthenticateAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication execution failed for request type: {RequestType}", request.AuthenticationType);
            return Abstractions.Interfaces.AuthenticationResult.Failure($"Authentication failed: {ex.Message}");
        }
    }
}