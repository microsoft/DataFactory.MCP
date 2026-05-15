using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.AirflowJob;

/// <summary>
/// Create Apache Airflow Job request payload
/// </summary>
public class CreateAirflowJobRequest
{
    /// <summary>
    /// The Apache Airflow Job display name. Maximum length is 256 characters.
    /// </summary>
    [JsonPropertyName("displayName")]
    [Required(ErrorMessage = "Display name is required")]
    [StringLength(256, ErrorMessage = "Display name cannot exceed 256 characters")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The Apache Airflow Job description. Maximum length is 256 characters.
    /// </summary>
    [JsonPropertyName("description")]
    [StringLength(256, ErrorMessage = "Description cannot exceed 256 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// The folder ID. If not specified or null, the item is created with the workspace as its folder.
    /// </summary>
    [JsonPropertyName("folderId")]
    public string? FolderId { get; set; }
}
