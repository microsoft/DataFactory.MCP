using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Pipeline;

/// <summary>
/// Update Pipeline request payload
/// </summary>
public class UpdatePipelineRequest
{
    /// <summary>
    /// The Pipeline display name.
    /// </summary>
    [JsonPropertyName("displayName")]
    [StringLength(256, ErrorMessage = "Display name cannot exceed 256 characters")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// The Pipeline description. Maximum length is 256 characters.
    /// </summary>
    [JsonPropertyName("description")]
    [StringLength(256, ErrorMessage = "Description cannot exceed 256 characters")]
    public string? Description { get; set; }
}
