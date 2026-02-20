using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Connection;

/// <summary>
/// Response from GET /v1/connections/supportedConnectionTypes
/// </summary>
public class ListSupportedConnectionTypesResponse
{
    [JsonPropertyName("value")]
    public List<ConnectionCreationMetadata> Value { get; set; } = [];

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }

    [JsonPropertyName("continuationUri")]
    public string? ContinuationUri { get; set; }
}

/// <summary>
/// Metadata for a supported connection type.
/// </summary>
public class ConnectionCreationMetadata
{
    /// <summary>The type of the connection (e.g. "SQL", "Web", "AzureBlobs").</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>A list of creation methods for the connection.</summary>
    [JsonPropertyName("creationMethods")]
    public List<ConnectionCreationMethod> CreationMethods { get; set; } = [];

    /// <summary>A list of credential type values that the connection supports.</summary>
    [JsonPropertyName("supportedCredentialTypes")]
    public List<string> SupportedCredentialTypes { get; set; } = [];

    /// <summary>A list of connection encryption values that the connection supports.</summary>
    [JsonPropertyName("supportedConnectionEncryptionTypes")]
    public List<string> SupportedConnectionEncryptionTypes { get; set; } = [];

    /// <summary>Whether the connection type supports skip test connection.</summary>
    [JsonPropertyName("supportsSkipTestConnection")]
    public bool SupportsSkipTestConnection { get; set; }
}

/// <summary>
/// A creation method for a connection type.
/// </summary>
public class ConnectionCreationMethod
{
    /// <summary>The name of the creation method.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>A list of creation method parameters for the connection.</summary>
    [JsonPropertyName("parameters")]
    public List<ConnectionCreationParameter> Parameters { get; set; } = [];
}

/// <summary>
/// A parameter required by a creation method.
/// </summary>
public class ConnectionCreationParameter
{
    /// <summary>The name of the connection creation parameter.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>The data type of the parameter (Text, Number, Boolean, etc.).</summary>
    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = "Text";

    /// <summary>Whether the parameter is required.</summary>
    [JsonPropertyName("required")]
    public bool Required { get; set; }

    /// <summary>A list of allowed values for the parameter.</summary>
    [JsonPropertyName("allowedValues")]
    public List<string>? AllowedValues { get; set; }
}
