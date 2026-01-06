using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Connection;

/// <summary>
/// The connectivity type of the connection
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConnectivityType
{
    ShareableCloud,
    PersonalCloud,
    OnPremisesGateway,
    OnPremisesGatewayPersonal,
    VirtualNetworkGateway,
    Automatic,
    None
}

/// <summary>
/// The credential type of the connection
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CredentialType
{
    Windows,
    Anonymous,
    Basic,
    Key,
    OAuth2,
    WindowsWithoutImpersonation,
    SharedAccessSignature,
    ServicePrincipal,
    WorkspaceIdentity
}

/// <summary>
/// The connection encryption type of the connection
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConnectionEncryption
{
    Encrypted,
    Any,
    NotEncrypted
}

/// <summary>
/// The privacy level setting of the connection
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PrivacyLevel
{
    None,
    Organizational,
    Public,
    Private
}

/// <summary>
/// The single sign-on type of the connection
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SingleSignOnType
{
    None,
    Kerberos,
    MicrosoftEntraID,
    SecurityAssertionMarkupLanguage,
    KerberosDirectQueryAndRefresh
}