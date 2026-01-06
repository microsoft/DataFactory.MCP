using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models;

namespace DataFactory.MCP.Tools;

[McpServerToolType]
public class AuthenticationTool
{
    private readonly IAuthenticationService _authService;
    private readonly IValidationService _validationService;

    public AuthenticationTool(IAuthenticationService authService, IValidationService validationService)
    {
        _authService = authService;
        _validationService = validationService;
    }

    [McpServerTool, Description(@"Authenticate with Azure AD using interactive login")]
    public async Task<string> AuthenticateInteractiveAsync()
    {
        try
        {
            return await _authService.AuthenticateInteractiveAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains(Messages.ServiceProviderNotInitialized))
        {
            return Messages.AuthServiceNotAvailable;
        }
        catch (Exception ex)
        {
            return string.Format(Messages.AuthenticationErrorTemplate, ex.Message);
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
            _validationService.ValidateRequiredString(applicationId, nameof(applicationId));
            _validationService.ValidateRequiredString(clientSecret, nameof(clientSecret));

            return await _authService.AuthenticateServicePrincipalAsync(applicationId, clientSecret, tenantId);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains(Messages.ServiceProviderNotInitialized))
        {
            return Messages.AuthServiceNotAvailable;
        }
        catch (Exception ex)
        {
            return string.Format(Messages.AuthenticationErrorTemplate, ex.Message);
        }
    }

    [McpServerTool, Description(@"Get current authentication status and profile information")]
    public string GetAuthenticationStatus()
    {
        try
        {
            return _authService.GetAuthenticationStatus();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains(Messages.ServiceProviderNotInitialized))
        {
            return Messages.AuthServiceNotAvailable;
        }
        catch (Exception ex)
        {
            return string.Format(Messages.ErrorRetrievingAuthStatusTemplate, ex.Message);
        }
    }

    [McpServerTool, Description(@"Clear current authentication and sign out")]
    public async Task<string> SignOutAsync()
    {
        try
        {
            return await _authService.SignOutAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains(Messages.ServiceProviderNotInitialized))
        {
            return Messages.AuthServiceNotAvailable;
        }
        catch (Exception ex)
        {
            return string.Format(Messages.SignOutErrorTemplate, ex.Message);
        }
    }

    [McpServerTool, Description(@"Get current access token for authenticated user")]
    public async Task<string> GetAccessTokenAsync()
    {
        try
        {
            return await _authService.GetAccessTokenAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains(Messages.ServiceProviderNotInitialized))
        {
            return Messages.AuthServiceNotAvailable;
        }
        catch (Exception ex)
        {
            return string.Format(Messages.ErrorRetrievingAccessTokenTemplate, ex.Message);
        }
    }

    [McpServerTool, Description(@"Get access token for Azure Resource Manager to access Azure subscriptions, resource groups, and virtual networks")]
    public async Task<string> GetAzureResourceManagerTokenAsync()
    {
        try
        {
            return await _authService.GetAccessTokenAsync(AzureAdConfiguration.AzureResourceManagerScopes);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains(Messages.ServiceProviderNotInitialized))
        {
            return Messages.AuthServiceNotAvailable;
        }
        catch (Exception ex)
        {
            return string.Format(Messages.ErrorRetrievingAccessTokenTemplate, ex.Message);
        }
    }
}
