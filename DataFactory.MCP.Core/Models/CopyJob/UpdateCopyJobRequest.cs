using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.CopyJob;

/// <summary>
/// Update Copy Job request payload
/// </summary>
public class UpdateCopyJobRequest
{
    /// <summary>
    /// The Copy Job display name.
    /// </summary>
    [JsonPropertyName("displayName")]
    [StringLength(256, ErrorMessage = "Display name cannot exceed 256 characters")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// The Copy Job description. Maximum length is 256 characters.
    /// </summary>
    [JsonPropertyName("description")]
    [StringLength(256, ErrorMessage = "Description cannot exceed 256 characters")]
    public string? Description { get; set; }
}
