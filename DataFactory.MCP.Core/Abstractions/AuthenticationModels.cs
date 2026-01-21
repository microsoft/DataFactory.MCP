namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Base class for authentication requests
/// </summary>
public abstract class AuthenticationRequest
{
    /// <summary>
    /// The type of authentication being requested
    /// </summary>
    public abstract string AuthenticationType { get; }

    /// <summary>
    /// Optional scopes to request during authentication
    /// </summary>
    public string[]? Scopes { get; set; }
}

/// <summary>
/// Request for interactive browser-based authentication
/// </summary>
public class InteractiveAuthenticationRequest : AuthenticationRequest
{
    public override string AuthenticationType => "Interactive";
}

/// <summary>
/// Request to start device code authentication
/// </summary>
public class StartDeviceCodeAuthenticationRequest : AuthenticationRequest
{
    public override string AuthenticationType => "StartDeviceCode";
}

/// <summary>
/// Request to check the status of pending device code authentication
/// </summary>
public class CheckDeviceCodeAuthenticationRequest : AuthenticationRequest
{
    public override string AuthenticationType => "CheckDeviceCode";
}

/// <summary>
/// Request for service principal authentication
/// </summary>
public class ServicePrincipalAuthenticationRequest : AuthenticationRequest
{
    public override string AuthenticationType => "ServicePrincipal";

    /// <summary>
    /// The application (client) ID of the service principal
    /// </summary>
    public required string ApplicationId { get; set; }

    /// <summary>
    /// The client secret for the service principal
    /// </summary>
    public required string ClientSecret { get; set; }

    /// <summary>
    /// Optional tenant ID. If not provided, uses default tenant from configuration
    /// </summary>
    public string? TenantId { get; set; }
}

/// <summary>
/// Result of an authentication attempt
/// </summary>
public class AuthenticationResult
{
    /// <summary>
    /// Whether the authentication was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// User-friendly message describing the result
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// The authentication details if successful
    /// </summary>
    public Models.McpAuthenticationResult? AuthenticationDetails { get; set; }

    /// <summary>
    /// Creates a successful authentication result
    /// </summary>
    public static AuthenticationResult Success(string message, Models.McpAuthenticationResult authDetails)
    {
        return new AuthenticationResult
        {
            IsSuccess = true,
            Message = message,
            AuthenticationDetails = authDetails
        };
    }

    /// <summary>
    /// Creates a failed authentication result
    /// </summary>
    public static AuthenticationResult Failure(string message)
    {
        return new AuthenticationResult
        {
            IsSuccess = false,
            Message = message,
            AuthenticationDetails = null
        };
    }
}