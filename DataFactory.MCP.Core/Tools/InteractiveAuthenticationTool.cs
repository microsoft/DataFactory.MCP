using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models;

namespace DataFactory.MCP.Tools;

/// <summary>
/// Tool for interactive authentication flow.
/// This is enabled via the interactive-auth feature flag.
/// Enabled by default for stdio, disabled by default for HTTP.
/// </summary>
[McpServerToolType]
public class InteractiveAuthenticationTool
{
    private readonly IAuthenticationService _authService;

    public InteractiveAuthenticationTool(IAuthenticationService authService)
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
        catch (InvalidOperationException ex) when (ex.Message.Contains(Messages.ServiceProviderNotInitialized))
        {
            return Messages.AuthServiceNotAvailable;
        }
        catch (Exception ex)
        {
            return string.Format(Messages.AuthenticationErrorTemplate, ex.Message);
        }
    }
}
