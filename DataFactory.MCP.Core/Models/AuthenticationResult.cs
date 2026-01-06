namespace DataFactory.MCP.Models;

/// <summary>
/// Represents the result of an authentication operation
/// </summary>
public class McpAuthenticationResult
{
    public bool IsSuccess { get; set; }
    public string? AccessToken { get; set; }
    public string? UserName { get; set; }
    public string? TenantId { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Scopes { get; set; }

    public static McpAuthenticationResult Success(string accessToken, string userName, string? tenantId = null, DateTime? expiresOn = null, string? scopes = null)
    {
        return new McpAuthenticationResult
        {
            IsSuccess = true,
            AccessToken = accessToken,
            UserName = userName,
            TenantId = tenantId,
            ExpiresOn = expiresOn,
            Scopes = scopes
        };
    }

    public static McpAuthenticationResult Failure(string errorMessage)
    {
        return new McpAuthenticationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
