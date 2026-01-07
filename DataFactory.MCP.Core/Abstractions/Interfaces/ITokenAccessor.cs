namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Provides access to the current authentication token.
/// This allows for per-request token injection in HTTP scenarios.
/// </summary>
public interface ITokenAccessor
{
    /// <summary>
    /// Gets the current access token, if available.
    /// </summary>
    string? AccessToken { get; }

    /// <summary>
    /// Sets the access token for the current scope/request.
    /// </summary>
    /// <param name="token">The access token to set</param>
    void SetAccessToken(string token);

    /// <summary>
    /// Returns true if an external token has been set.
    /// </summary>
    bool HasExternalToken { get; }
}
