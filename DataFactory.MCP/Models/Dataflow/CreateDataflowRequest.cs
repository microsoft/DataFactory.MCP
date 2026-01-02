using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DataFactory.MCP.Models.Dataflow.Definition;

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
