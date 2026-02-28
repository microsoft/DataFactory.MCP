using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.CopyJob.Definition;

/// <summary>
/// Copy Job public definition object
/// </summary>
public class CopyJobDefinition
{
    /// <summary>
    /// A list of definition parts
    /// </summary>
    [JsonPropertyName("parts")]
    [Required(ErrorMessage = "Definition parts are required")]
    public List<CopyJobDefinitionPart> Parts { get; set; } = new();
}
