using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Connection;

/// <summary>
/// Request model for creating a new connection via the Fabric Connections API.
/// POST https://api.fabric.microsoft.com/v1/connections
/// </summary>
public class CreateConnectionRequest
{
    /// <summary>
    /// The connectivity type of the connection.
    /// </summary>
    [JsonPropertyName("connectivityType")]
    public string ConnectivityType { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the connection.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The connection details containing type and path.
    /// </summary>
    [JsonPropertyName("connectionDetails")]
    public CreateConnectionDetails ConnectionDetails { get; set; } = new();

    /// <summary>
    /// The credential details for the connection.
    /// </summary>
    [JsonPropertyName("credentialDetails")]
    public CreateCredentialDetails CredentialDetails { get; set; } = new();

    /// <summary>
    /// The privacy level setting. Optional.
    /// </summary>
    [JsonPropertyName("privacyLevel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PrivacyLevel { get; set; }

    /// <summary>
    /// The gateway ID for on-premises or VNet connections. Optional.
    /// </summary>
    [JsonPropertyName("gatewayId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GatewayId { get; set; }
}

/// <summary>
/// Connection details for create request.
/// </summary>
public class CreateConnectionDetails
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The creation method used to create the connection.
    /// For a list of creation methods use the ListSupportedConnectionTypes API.
    /// </summary>
    [JsonPropertyName("creationMethod")]
    public string CreationMethod { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<CreateConnectionParameter>? Parameters { get; set; }
}

/// <summary>
/// A parameter for connection details.
/// </summary>
public class CreateConnectionParameter
{
    /// <summary>
    /// The data type of the parameter (Text, Number, Boolean, etc.).
    /// </summary>
    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = "Text";

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Credential details for create request.
/// </summary>
public class CreateCredentialDetails
{
    [JsonPropertyName("singleSignOnType")]
    public string SingleSignOnType { get; set; } = "None";

    [JsonPropertyName("connectionEncryption")]
    public string ConnectionEncryption { get; set; } = "NotEncrypted";

    [JsonPropertyName("skipTestConnection")]
    public bool SkipTestConnection { get; set; } = true;

    [JsonPropertyName("credentials")]
    public CreateCredentials Credentials { get; set; } = new();
}

/// <summary>
/// Credentials object for create request.
/// Supports multiple credential types: Anonymous, Basic, Key, OAuth2, SAS, ServicePrincipal, Windows.
/// </summary>
public class CreateCredentials
{
    [JsonPropertyName("credentialType")]
    public string CredentialType { get; set; } = "Anonymous";

    // Basic / Windows credentials
    [JsonPropertyName("username")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Username { get; set; }

    [JsonPropertyName("password")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Password { get; set; }

    // Key credentials (e.g., Azure Storage account key)
    [JsonPropertyName("key")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Key { get; set; }

    // SAS credentials
    [JsonPropertyName("token")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Token { get; set; }

    // Service Principal credentials
    [JsonPropertyName("tenantId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TenantId { get; set; }

    [JsonPropertyName("servicePrincipalClientId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ServicePrincipalClientId { get; set; }

    [JsonPropertyName("servicePrincipalSecret")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ServicePrincipalSecret { get; set; }
}
