using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Configuration;
using DataFactory.MCP.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace DataFactory.MCP.Services.Authentication;

/// <summary>
/// Handles service principal (client credentials) authentication
/// </summary>
public class ServicePrincipalAuthenticationProvider : IAuthenticationProvider
{
    private readonly ILogger<ServicePrincipalAuthenticationProvider> _logger;

    public ServicePrincipalAuthenticationProvider(ILogger<ServicePrincipalAuthenticationProvider> logger)
    {
        _logger = logger;
    }

    public string ProviderType => "ServicePrincipal";

    public bool CanHandle(AuthenticationRequest request)
    {
        return request is ServicePrincipalAuthenticationRequest;
    }

    public async Task<Abstractions.Interfaces.AuthenticationResult> AuthenticateAsync(AuthenticationRequest request)
    {
        if (request is not ServicePrincipalAuthenticationRequest spRequest)
        {
            return Abstractions.Interfaces.AuthenticationResult.Failure("Invalid request type for Service Principal authentication");
        }

        try
        {
            _logger.LogInformation("Starting service principal authentication for app: {ApplicationId}", spRequest.ApplicationId);

            // Create a confidential client application for this specific authentication
            var authority = $"https://login.microsoftonline.com/{spRequest.TenantId ?? AzureAdConfiguration.TenantId}";

            var confidentialClient = ConfidentialClientApplicationBuilder
                .Create(spRequest.ApplicationId)
                .WithClientSecret(spRequest.ClientSecret)
                .WithAuthority(authority)
                .Build();

            var scopes = request.Scopes ?? AzureAdConfiguration.PowerBIScopes;
            var result = await confidentialClient
                .AcquireTokenForClient(scopes)
                .ExecuteAsync();

            var authDetails = McpAuthenticationResult.Success(
                result.AccessToken,
                $"ServicePrincipal-{spRequest.ApplicationId}",
                spRequest.TenantId ?? AzureAdConfiguration.TenantId,
                result.ExpiresOn.DateTime,
                string.Join(", ", result.Scopes)
            );

            _logger.LogInformation("Service principal authentication completed successfully for app: {ApplicationId}", spRequest.ApplicationId);

            var message = string.Format(Messages.ServicePrincipalAuthenticationSuccessTemplate, spRequest.ApplicationId);
            return Abstractions.Interfaces.AuthenticationResult.Success(message, authDetails);
        }
        catch (MsalException msalEx)
        {
            _logger.LogError(msalEx, "MSAL service principal authentication failed for app: {ApplicationId}", spRequest.ApplicationId);
            return Abstractions.Interfaces.AuthenticationResult.Failure(string.Format(Messages.ServicePrincipalAuthenticationFailedTemplate, msalEx.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service principal authentication failed for app: {ApplicationId}", spRequest.ApplicationId);
            return Abstractions.Interfaces.AuthenticationResult.Failure(string.Format(Messages.AuthenticationErrorTemplate, ex.Message));
        }
    }
}