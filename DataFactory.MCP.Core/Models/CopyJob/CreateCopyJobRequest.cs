using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.CopyJob;

/// <summary>
/// Create Copy Job request payload
/// </summary>
public class CreateCopyJobRequest
{
    /// <summary>
    /// The Copy Job display name. The display name must follow naming rules according to item type.
    /// </summary>
    [JsonPropertyName("displayName")]
    [Required(ErrorMessage = "Display name is required")]
    [StringLength(256, ErrorMessage = "Display name cannot exceed 256 characters")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The Copy Job description. Maximum length is 256 characters.
    /// </summary>
    [JsonPropertyName("description")]
    [StringLength(256, ErrorMessage = "Description cannot exceed 256 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// The folder ID. If not specified or null, the Copy Job is created with the workspace as its folder.
    /// </summary>
    [JsonPropertyName("folderId")]
    public string? FolderId { get; set; }
}
