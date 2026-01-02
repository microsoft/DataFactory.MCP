using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Dataflow.Definition;

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
