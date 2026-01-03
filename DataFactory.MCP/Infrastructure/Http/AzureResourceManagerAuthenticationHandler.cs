using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace DataFactory.MCP.Infrastructure.Http;

/// <summary>
/// Authentication handler specifically for Azure Resource Manager API with ARM scopes
/// </summary>
public class AzureResourceManagerAuthenticationHandler : DelegatingHandler
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AzureResourceManagerAuthenticationHandler> _logger;

    public AzureResourceManagerAuthenticationHandler(
        IAuthenticationService authService,
        ILogger<AzureResourceManagerAuthenticationHandler> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await _authService.GetAccessTokenAsync(AzureAdConfiguration.AzureResourceManagerScopes);

            TokenValidator.ValidateToken(token);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set authentication header for Azure Resource Manager request to {Url}", request.RequestUri);
            throw new UnauthorizedAccessException(Messages.AuthenticationRequired, ex);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
