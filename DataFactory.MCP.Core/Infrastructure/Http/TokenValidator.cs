using DataFactory.MCP.Models;

namespace DataFactory.MCP.Infrastructure.Http;

/// <summary>
/// Shared token validation logic for authentication handlers
/// </summary>
public static class TokenValidator
{
    /// <summary>
    /// Validates that an access token is properly formatted and usable.
    /// Throws UnauthorizedAccessException if the token is invalid.
    /// </summary>
    /// <param name="token">The token to validate</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when token is invalid</exception>
    public static void ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new UnauthorizedAccessException(Messages.AuthenticationRequired);
        }

        // Check for error messages returned instead of tokens
        if (token.Contains("No valid authentication") ||
            token.Contains("expired") ||
            token.Contains("Error") ||
            token.Contains("Failed"))
        {
            throw new UnauthorizedAccessException(Messages.AuthenticationRequired);
        }

        // Basic JWT token validation - JWT tokens start with "eyJ"
        if (!token.StartsWith("eyJ"))
        {
            throw new UnauthorizedAccessException(Messages.InvalidTokenFormat);
        }
    }
}
