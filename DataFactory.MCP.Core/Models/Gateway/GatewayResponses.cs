using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Gateway;

/// <summary>
/// Response for listing gateways
/// </summary>
public class ListGatewaysResponse
{
    /// <summary>
    /// A list of gateways returned
    /// </summary>
    [JsonPropertyName("value")]
    public List<Gateway> Value { get; set; } = new();

    /// <summary>
    /// The token for the next result set batch
    /// </summary>
    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }

    /// <summary>
    /// The URI of the next result set batch
    /// </summary>
    [JsonPropertyName("continuationUri")]
    public string? ContinuationUri { get; set; }
}

/// <summary>
/// Error response from Fabric API
/// </summary>
public class FabricErrorResponse
{
    /// <summary>
    /// A specific identifier that provides information about an error condition
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// A human readable representation of the error
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// ID of the request associated with the error
    /// </summary>
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;
}
