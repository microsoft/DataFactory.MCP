using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Dataflow;

/// <summary>
/// Represents a Dataflow object in Microsoft Fabric
/// </summary>
public class Dataflow
{
    /// <summary>
    /// The item ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The item display name
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The item description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The item type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The workspace ID
    /// </summary>
    [JsonPropertyName("workspaceId")]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>
    /// The folder ID
    /// </summary>
    [JsonPropertyName("folderId")]
    public string? FolderId { get; set; }

    /// <summary>
    /// List of applied tags
    /// </summary>
    [JsonPropertyName("tags")]
    public List<ItemTag>? Tags { get; set; }

    /// <summary>
    /// Additional properties of the dataflow
    /// </summary>
    [JsonPropertyName("properties")]
    public DataflowProperties? Properties { get; set; }
}