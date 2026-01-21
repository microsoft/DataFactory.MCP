using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Configuration;
using DataFactory.MCP.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace DataFactory.MCP.Services.Authentication;

/// <summary>
/// Handles interactive browser-based authentication
/// </summary>
public class InteractiveAuthenticationProvider : IAuthenticationProvider
{
    private readonly ILogger<InteractiveAuthenticationProvider> _logger;
    private readonly IPublicClientApplication _publicClientApp;

    public InteractiveAuthenticationProvider(ILogger<InteractiveAuthenticationProvider> logger)
    {
        _logger = logger;
        _publicClientApp = CreatePublicClientApp();
    }

    public string ProviderType => "Interactive";

    public bool CanHandle(AuthenticationRequest request)
    {
        return request is InteractiveAuthenticationRequest;
    }

    public async Task<Abstractions.Interfaces.AuthenticationResult> AuthenticateAsync(AuthenticationRequest request)
    {
        if (!CanHandle(request))
        {
            return Abstractions.Interfaces.AuthenticationResult.Failure("Invalid request type for Interactive authentication");
        }

        try
        {
            _logger.LogInformation(Messages.StartingInteractiveAuthentication);

            var scopes = request.Scopes ?? AzureAdConfiguration.PowerBIScopes;
            var result = await _publicClientApp
                .AcquireTokenInteractive(scopes)
                .ExecuteAsync();

            var authDetails = McpAuthenticationResult.Success(
                result.AccessToken,
                result.Account.Username,
                result.TenantId,
                result.ExpiresOn.DateTime,
                string.Join(", ", result.Scopes)
            );

            _logger.LogInformation("Interactive authentication completed successfully for user: {Username}", result.Account.Username);

            var message = string.Format(Messages.InteractiveAuthenticationSuccessTemplate, result.Account.Username);
            return Abstractions.Interfaces.AuthenticationResult.Success(message, authDetails);
        }
        catch (MsalException msalEx)
        {
            _logger.LogError(msalEx, "MSAL authentication failed");
            return Abstractions.Interfaces.AuthenticationResult.Failure(string.Format(Messages.AuthenticationFailedTemplate, msalEx.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Interactive authentication failed");
            return Abstractions.Interfaces.AuthenticationResult.Failure(string.Format(Messages.AuthenticationErrorTemplate, ex.Message));
        }
    }

    private static IPublicClientApplication CreatePublicClientApp()
    {
        return PublicClientApplicationBuilder
            .Create(AzureAdConfiguration.ClientId)
            .WithAuthority(new Uri(AzureAdConfiguration.Authority))
            .WithRedirectUri(AzureAdConfiguration.RedirectUri)
            .Build();
    }
}