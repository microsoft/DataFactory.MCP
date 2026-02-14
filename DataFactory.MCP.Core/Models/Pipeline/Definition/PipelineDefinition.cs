using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Pipeline.Definition;

/// <summary>
/// Pipeline public definition object
/// </summary>
public class PipelineDefinition
{
    /// <summary>
    /// A list of definition parts
    /// </summary>
    [JsonPropertyName("parts")]
    [Required(ErrorMessage = "Definition parts are required")]
    public List<PipelineDefinitionPart> Parts { get; set; } = new();
}
