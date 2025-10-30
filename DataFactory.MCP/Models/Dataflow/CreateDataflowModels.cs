using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Dataflow;

/// <summary>
/// Create Dataflow request payload
/// </summary>
public class CreateDataflowRequest
{
    /// <summary>
    /// The Dataflow display name. The display name must follow naming rules according to item type.
    /// </summary>
    [JsonPropertyName("displayName")]
    [Required(ErrorMessage = "Display name is required")]
    [StringLength(256, ErrorMessage = "Display name cannot exceed 256 characters")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The Dataflow description. Maximum length is 256 characters.
    /// </summary>
    [JsonPropertyName("description")]
    [StringLength(256, ErrorMessage = "Description cannot exceed 256 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// The folder ID. If not specified or null, the Dataflow is created with the workspace as its folder.
    /// </summary>
    [JsonPropertyName("folderId")]
    public string? FolderId { get; set; }

    /// <summary>
    /// The Dataflow public definition.
    /// </summary>
    [JsonPropertyName("definition")]
    public DataflowDefinition? Definition { get; set; }
}

/// <summary>
/// Dataflow public definition object
/// </summary>
public class DataflowDefinition
{
    /// <summary>
    /// A list of definition parts
    /// </summary>
    [JsonPropertyName("parts")]
    [Required(ErrorMessage = "Definition parts are required")]
    public List<DataflowDefinitionPart> Parts { get; set; } = new();
}

/// <summary>
/// Dataflow definition part object
/// </summary>
public class DataflowDefinitionPart
{
    /// <summary>
    /// The Dataflow public definition part path
    /// </summary>
    [JsonPropertyName("path")]
    [Required(ErrorMessage = "Path is required")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// The Dataflow public definition part payload
    /// </summary>
    [JsonPropertyName("payload")]
    [Required(ErrorMessage = "Payload is required")]
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// The payload type
    /// </summary>
    [JsonPropertyName("payloadType")]
    public PayloadType PayloadType { get; set; } = PayloadType.InlineBase64;
}

/// <summary>
/// The type of the definition part payload
/// </summary>
public enum PayloadType
{
    /// <summary>
    /// Inline Base 64
    /// </summary>
    [JsonPropertyName("InlineBase64")]
    InlineBase64
}

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