using DataFactory.MCP.Abstractions.Interfaces;

namespace DataFactory.MCP.Http.Middleware;

/// <summary>
/// Middleware that extracts Bearer tokens from incoming HTTP requests
/// and makes them available to the authentication service for OAuth passthrough.
/// </summary>
public class BearerTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BearerTokenMiddleware> _logger;

    public BearerTokenMiddleware(RequestDelegate next, ILogger<BearerTokenMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITokenAccessor tokenAccessor)
    {
        // Extract Bearer token from Authorization header
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();

            if (!string.IsNullOrEmpty(token))
            {
                _logger.LogDebug("Bearer token extracted from request, setting in token accessor");
                tokenAccessor.SetAccessToken(token);
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for adding the Bearer token middleware
/// </summary>
public static class BearerTokenMiddlewareExtensions
{
    /// <summary>
    /// Adds middleware that extracts Bearer tokens from incoming requests
    /// for OAuth passthrough authentication to Fabric APIs.
    /// </summary>
    public static IApplicationBuilder UseBearerTokenPassthrough(this IApplicationBuilder app)
    {
        return app.UseMiddleware<BearerTokenMiddleware>();
    }
}
