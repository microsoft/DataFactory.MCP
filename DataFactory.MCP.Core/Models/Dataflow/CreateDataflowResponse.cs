using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Dataflow;

/// <summary>
/// Create Dataflow response
/// </summary>
public class CreateDataflowResponse
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
}
