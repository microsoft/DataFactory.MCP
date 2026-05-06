using System.Text.Json.Serialization;

namespace Fabric.Mcp.Tools.DataFactory.Models.Connection;

/// <summary>
/// The connectivity type of the connection
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ConnectivityType>))]
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
[JsonConverter(typeof(JsonStringEnumConverter<CredentialType>))]
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
[JsonConverter(typeof(JsonStringEnumConverter<ConnectionEncryption>))]
public enum ConnectionEncryption
{
    Encrypted,
    Any,
    NotEncrypted
}

/// <summary>
/// The privacy level setting of the connection
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PrivacyLevel>))]
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
[JsonConverter(typeof(JsonStringEnumConverter<SingleSignOnType>))]
public enum SingleSignOnType
{
    None,
    Kerberos,
    MicrosoftEntraID,
    SecurityAssertionMarkupLanguage,
    KerberosDirectQueryAndRefresh
}