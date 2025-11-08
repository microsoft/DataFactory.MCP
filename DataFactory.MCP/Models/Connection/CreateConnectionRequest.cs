using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Connection;

/// <summary>
/// Base request for creating connections
/// </summary>
public abstract class CreateConnectionRequest
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("connectivityType")]
    public ConnectivityType ConnectivityType { get; set; }

    [JsonPropertyName("connectionDetails")]
    public CreateConnectionDetails ConnectionDetails { get; set; } = new();

    [JsonPropertyName("privacyLevel")]
    public PrivacyLevel? PrivacyLevel { get; set; }

    [JsonPropertyName("credentialDetails")]
    public CreateCredentialDetails CredentialDetails { get; set; } = new();
}

/// <summary>
/// Request for creating cloud connections
/// </summary>
public class CreateCloudConnectionRequest : CreateConnectionRequest
{
    [JsonPropertyName("allowConnectionUsageInGateway")]
    public bool? AllowConnectionUsageInGateway { get; set; }

    public CreateCloudConnectionRequest()
    {
        ConnectivityType = ConnectivityType.ShareableCloud;
    }
}

/// <summary>
/// Request for creating virtual network gateway connections
/// </summary>
public class CreateVirtualNetworkGatewayConnectionRequest : CreateConnectionRequest
{
    [JsonPropertyName("gatewayId")]
    public string GatewayId { get; set; } = string.Empty;

    public CreateVirtualNetworkGatewayConnectionRequest()
    {
        ConnectivityType = ConnectivityType.VirtualNetworkGateway;
    }
}

/// <summary>
/// The connection details input for create operations
/// </summary>
public class CreateConnectionDetails
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("creationMethod")]
    public string CreationMethod { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public List<ConnectionDetailsParameter> Parameters { get; set; } = new();
}

/// <summary>
/// The credential details input for creating a connection
/// </summary>
public class CreateCredentialDetails
{
    [JsonPropertyName("singleSignOnType")]
    public SingleSignOnType SingleSignOnType { get; set; }

    [JsonPropertyName("connectionEncryption")]
    public ConnectionEncryption ConnectionEncryption { get; set; }

    [JsonPropertyName("skipTestConnection")]
    public bool SkipTestConnection { get; set; }

    [JsonPropertyName("credentials")]
    public Credentials? Credentials { get; set; }
}

/// <summary>
/// Base class for connection parameters
/// </summary>
public abstract class ConnectionDetailsParameter
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = string.Empty;
}

/// <summary>
/// ConnectionDetailsParameter for text dataType
/// </summary>
public class ConnectionDetailsTextParameter : ConnectionDetailsParameter
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    public ConnectionDetailsTextParameter()
    {
        DataType = "Text";
    }
}

/// <summary>
/// ConnectionDetailsParameter for number dataType
/// </summary>
public class ConnectionDetailsNumberParameter : ConnectionDetailsParameter
{
    [JsonPropertyName("value")]
    public double Value { get; set; }

    public ConnectionDetailsNumberParameter()
    {
        DataType = "Number";
    }
}

/// <summary>
/// ConnectionDetailsParameter for boolean dataType
/// </summary>
public class ConnectionDetailsBooleanParameter : ConnectionDetailsParameter
{
    [JsonPropertyName("value")]
    public bool Value { get; set; }

    public ConnectionDetailsBooleanParameter()
    {
        DataType = "Boolean";
    }
}

/// <summary>
/// Base class for credentials
/// </summary>
public abstract class Credentials
{
    [JsonPropertyName("credentialType")]
    public CredentialType CredentialType { get; set; }
}

/// <summary>
/// Credentials for Basic CredentialType
/// </summary>
public class BasicCredentials : Credentials
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    public BasicCredentials()
    {
        CredentialType = CredentialType.Basic;
    }
}

/// <summary>
/// Credentials for Anonymous CredentialType
/// </summary>
public class AnonymousCredentials : Credentials
{
    public AnonymousCredentials()
    {
        CredentialType = CredentialType.Anonymous;
    }
}

/// <summary>
/// Credentials for OAuth2 CredentialType
/// </summary>
public class OAuth2Credentials : Credentials
{
    public OAuth2Credentials()
    {
        CredentialType = CredentialType.OAuth2;
    }
}

/// <summary>
/// Credentials for WorkspaceIdentity CredentialType
/// </summary>
public class WorkspaceIdentityCredentials : Credentials
{
    public WorkspaceIdentityCredentials()
    {
        CredentialType = CredentialType.WorkspaceIdentity;
    }
}