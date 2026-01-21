using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models;

namespace DataFactory.MCP.Tools;

/// <summary>
/// Tool for device code authentication flow.
/// This is enabled via the device-code-auth feature flag and is only available in the HTTP version.
/// </summary>
[McpServerToolType]
public class DeviceCodeAuthenticationTool
{
    private readonly IAuthenticationService _authService;

    public DeviceCodeAuthenticationTool(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [McpServerTool, Description(@"Start device code authentication - returns device code and URL immediately for user action")]
    public async Task<string> StartDeviceCodeAuthAsync()
    {
        try
        {
            return await _authService.StartDeviceCodeAuthAsync();
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

    [McpServerTool, Description(@"Check the status of pending device code authentication")]
    public async Task<string> CheckDeviceAuthStatusAsync()
    {
        try
        {
            return await _authService.CheckDeviceAuthStatusAsync();
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
