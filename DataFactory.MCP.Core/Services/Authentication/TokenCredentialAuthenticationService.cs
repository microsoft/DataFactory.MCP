using Azure.Core;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Services.Authentication;

/// <summary>
/// An IAuthenticationService implementation that delegates token acquisition to an Azure.Core TokenCredential.
/// Used when DataFactory.MCP.Core is hosted inside a system that already provides authentication
/// (e.g., Fabric MCP Server with DefaultAzureCredential).
/// </summary>
public class TokenCredentialAuthenticationService : IAuthenticationService
{
    private readonly TokenCredential _credential;
    private readonly ILogger<TokenCredentialAuthenticationService> _logger;

    public TokenCredentialAuthenticationService(
        TokenCredential credential,
        ILogger<TokenCredentialAuthenticationService> logger)
    {
        _credential = credential;
        _logger = logger;
    }

    public Task<string> GetAccessTokenAsync()
    {
        return GetAccessTokenAsync(AzureAdConfiguration.PowerBIScopes);
    }

    public async Task<string> GetAccessTokenAsync(string[] scopes)
    {
        var context = new TokenRequestContext(scopes);
        var token = await _credential.GetTokenAsync(context, CancellationToken.None).ConfigureAwait(false);
        return token.Token;
    }

    public string GetAuthenticationStatus()
    {
        return "Authenticated via host-provided credential (TokenCredential)";
    }

    public Task<string> AuthenticateInteractiveAsync()
    {
        _logger.LogInformation("Interactive authentication is not required — using host-provided credential");
        return Task.FromResult("Already authenticated via host-provided credential. No login required.");
    }

    public Task<string> StartDeviceCodeAuthAsync()
    {
        _logger.LogInformation("Device code authentication is not required — using host-provided credential");
        return Task.FromResult("Already authenticated via host-provided credential. No device code required.");
    }

    public Task<string> CheckDeviceAuthStatusAsync()
    {
        return Task.FromResult("Already authenticated via host-provided credential.");
    }

    public Task<string> AuthenticateServicePrincipalAsync(string applicationId, string clientSecret, string? tenantId = null)
    {
        _logger.LogInformation("Service principal authentication is not required — using host-provided credential");
        return Task.FromResult("Already authenticated via host-provided credential. No service principal login required.");
    }

    public Task<string> SignOutAsync()
    {
        _logger.LogInformation("Sign out is not applicable — authentication is managed by the host");
        return Task.FromResult("Authentication is managed by the host. Sign out is not applicable.");
    }
}
