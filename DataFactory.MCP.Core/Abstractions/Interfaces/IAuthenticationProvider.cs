namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Base interface for authentication providers that handle specific authentication methods
/// </summary>
public interface IAuthenticationProvider
{
    /// <summary>
    /// The unique identifier for this authentication provider type
    /// </summary>
    string ProviderType { get; }

    /// <summary>
    /// Determines if this provider can handle the given authentication request
    /// </summary>
    /// <param name="request">The authentication request to evaluate</param>
    /// <returns>True if this provider can handle the request</returns>
    bool CanHandle(AuthenticationRequest request);

    /// <summary>
    /// Performs authentication using this provider's method
    /// </summary>
    /// <param name="request">The authentication request containing necessary parameters</param>
    /// <returns>Authentication result with success/failure status and details</returns>
    Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request);
}