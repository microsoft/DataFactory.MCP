using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace DataFactory.MCP.Infrastructure.Http;

/// <summary>
/// A delegating handler that automatically adds authentication headers to outgoing HTTP requests.
/// This centralizes authentication logic that was previously duplicated across services.
/// </summary>
public class FabricAuthenticationHandler : DelegatingHandler
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<FabricAuthenticationHandler> _logger;
    private readonly string[]? _scopes;

    /// <summary>
    /// Creates a new authentication handler for Fabric API (default scopes)
    /// </summary>
    public FabricAuthenticationHandler(
        IAuthenticationService authService,
        ILogger<FabricAuthenticationHandler> logger)
        : this(authService, logger, scopes: null)
    {
    }

    /// <summary>
    /// Creates a new authentication handler with custom scopes
    /// </summary>
    public FabricAuthenticationHandler(
        IAuthenticationService authService,
        ILogger<FabricAuthenticationHandler> logger,
        string[]? scopes)
    {
        _authService = authService;
        _logger = logger;
        _scopes = scopes;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = _scopes != null
                ? await _authService.GetAccessTokenAsync(_scopes)
                : await _authService.GetAccessTokenAsync();

            TokenValidator.ValidateToken(token);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        catch (UnauthorizedAccessException)
        {
            // Re-throw auth exceptions as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set authentication header for request to {Url}", request.RequestUri);
            throw new UnauthorizedAccessException(Messages.AuthenticationRequired, ex);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
