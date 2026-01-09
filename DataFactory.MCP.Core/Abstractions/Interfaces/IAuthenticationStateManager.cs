using DataFactory.MCP.Models;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Manages authentication state, tokens, and provides access token retrieval
/// </summary>
public interface IAuthenticationStateManager
{
    /// <summary>
    /// Gets the current authentication result if user is authenticated
    /// </summary>
    McpAuthenticationResult? CurrentAuthentication { get; }

    /// <summary>
    /// Sets the current authentication state
    /// </summary>
    /// <param name="result">The authentication result to store</param>
    void SetAuthentication(McpAuthenticationResult result);

    /// <summary>
    /// Clears the current authentication state
    /// </summary>
    void ClearAuthentication();

    /// <summary>
    /// Gets the current authentication status as a formatted string
    /// </summary>
    /// <returns>Human-readable authentication status</returns>
    string GetAuthenticationStatus();

    /// <summary>
    /// Gets an access token for the current authentication, refreshing if necessary
    /// </summary>
    /// <param name="scopes">Optional specific scopes to request. If null, uses default scopes.</param>
    /// <returns>Access token or error message</returns>
    Task<string> GetAccessTokenAsync(string[]? scopes = null);

    /// <summary>
    /// Signs out the current user and clears authentication state
    /// </summary>
    /// <returns>Sign out result message</returns>
    Task<string> SignOutAsync();
}