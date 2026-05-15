using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.AirflowJob.Definition;

/// <summary>
/// Apache Airflow Job public definition object
/// </summary>
public class AirflowJobDefinition
{
    /// <summary>
    /// The definition format. Optional — used when requesting a specific format.
    /// </summary>
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    /// <summary>
    /// A list of definition parts
    /// </summary>
    [JsonPropertyName("parts")]
    [Required(ErrorMessage = "Definition parts are required")]
    public List<AirflowJobDefinitionPart> Parts { get; set; } = new();
}
