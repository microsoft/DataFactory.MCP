using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;

namespace DataFactory.MCP.Tools;

[McpServerToolType]
public class AuthenticationTool
{
    private const string ServiceProviderNotInitializedMessage = "Service provider not initialized";
    private const string AuthServiceNotAvailableMessage = "Error: Authentication service not available. Please ensure the server is properly initialized.";

    private readonly IAuthenticationService _authService;

    public AuthenticationTool(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [McpServerTool, Description(@"Authenticate with Azure AD using interactive login")]
    public async Task<string> AuthenticateInteractiveAsync()
    {
        try
        {
            return await _authService.AuthenticateInteractiveAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains(ServiceProviderNotInitializedMessage))
        {
            return AuthServiceNotAvailableMessage;
        }
        catch (Exception ex)
        {
            return $"Authentication error: {ex.Message}";
        }
    }

    [McpServerTool, Description(@"Authenticate with Azure AD using service principal and client secret")]
    public async Task<string> AuthenticateServicePrincipalAsync(
        string applicationId,
        string clientSecret,
        string? tenantId = null)
    {
        try
        {
            // Validate parameters
            if (string.IsNullOrWhiteSpace(applicationId))
                return "Invalid parameter: applicationId cannot be empty";

            if (string.IsNullOrWhiteSpace(clientSecret))
                return "Invalid parameter: clientSecret cannot be empty";

            return await _authService.AuthenticateServicePrincipalAsync(applicationId, clientSecret, tenantId);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains(ServiceProviderNotInitializedMessage))
        {
            return AuthServiceNotAvailableMessage;
        }
        catch (Exception ex)
        {
            return $"Authentication error: {ex.Message}";
        }
    }

    [McpServerTool, Description(@"Get current authentication status and profile information")]
    public string GetAuthenticationStatus()
    {
        try
        {
            return _authService.GetAuthenticationStatus();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains(ServiceProviderNotInitializedMessage))
        {
            return AuthServiceNotAvailableMessage;
        }
        catch (Exception ex)
        {
            return $"Error retrieving authentication status: {ex.Message}";
        }
    }

    [McpServerTool, Description(@"Clear current authentication and sign out")]
    public async Task<string> SignOutAsync()
    {
        try
        {
            return await _authService.SignOutAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains(ServiceProviderNotInitializedMessage))
        {
            return AuthServiceNotAvailableMessage;
        }
        catch (Exception ex)
        {
            return $"Sign out error: {ex.Message}";
        }
    }

    [McpServerTool, Description(@"Get current access token for authenticated user")]
    public async Task<string> GetAccessTokenAsync()
    {
        try
        {
            return await _authService.GetAccessTokenAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains(ServiceProviderNotInitializedMessage))
        {
            return AuthServiceNotAvailableMessage;
        }
        catch (Exception ex)
        {
            return $"Error retrieving access token: {ex.Message}";
        }
    }
}
