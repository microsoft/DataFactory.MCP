using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Workspace;

/// <summary>
/// A workspace object representing a Microsoft Fabric workspace
/// </summary>
public class Workspace
{
    /// <summary>
    /// The workspace ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The workspace display name
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The workspace description
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The workspace type
    /// </summary>
    [JsonPropertyName("type")]
    public WorkspaceType Type { get; set; }

    /// <summary>
    /// The ID of the capacity the workspace is assigned to
    /// </summary>
    [JsonPropertyName("capacityId")]
    public string? CapacityId { get; set; }

    /// <summary>
    /// The ID of the domain the workspace is assigned to
    /// </summary>
    [JsonPropertyName("domainId")]
    public string? DomainId { get; set; }

    /// <summary>
    /// HTTP URL that represents the API endpoint specific to the workspace.
    /// This endpoint value is returned when the user enables preferWorkspaceSpecificEndpoints.
    /// It allows for API access over private links.
    /// </summary>
    [JsonPropertyName("apiEndpoint")]
    public string? ApiEndpoint { get; set; }
}