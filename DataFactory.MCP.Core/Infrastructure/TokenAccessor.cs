using DataFactory.MCP.Abstractions.Interfaces;

namespace DataFactory.MCP.Infrastructure;

/// <summary>
/// Default token accessor that stores tokens in AsyncLocal for per-request isolation.
/// This allows tokens to be passed through from HTTP middleware to services.
/// </summary>
public class TokenAccessor : ITokenAccessor
{
    private static readonly AsyncLocal<string?> _asyncLocalToken = new();

    /// <inheritdoc />
    public string? AccessToken => _asyncLocalToken.Value;

    /// <inheritdoc />
    public bool HasExternalToken => !string.IsNullOrEmpty(_asyncLocalToken.Value);

    /// <inheritdoc />
    public void SetAccessToken(string token)
    {
        _asyncLocalToken.Value = token;
    }

    /// <summary>
    /// Clears the current token (useful for cleanup after request).
    /// </summary>
    public void ClearToken()
    {
        _asyncLocalToken.Value = null;
    }
}
