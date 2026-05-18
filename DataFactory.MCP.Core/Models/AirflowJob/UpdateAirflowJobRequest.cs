using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.AirflowJob;

/// <summary>
/// Update Apache Airflow Job request payload
/// </summary>
public class UpdateAirflowJobRequest
{
    /// <summary>
    /// The Apache Airflow Job display name. Maximum length is 256 characters.
    /// </summary>
    [JsonPropertyName("displayName")]
    [StringLength(256, ErrorMessage = "Display name cannot exceed 256 characters")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// The Apache Airflow Job description. Maximum length is 256 characters.
    /// </summary>
    [JsonPropertyName("description")]
    [StringLength(256, ErrorMessage = "Description cannot exceed 256 characters")]
    public string? Description { get; set; }
}
