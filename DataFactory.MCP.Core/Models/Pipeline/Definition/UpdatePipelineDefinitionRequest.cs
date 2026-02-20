using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Pipeline.Definition;

/// <summary>
/// Request model for updating pipeline definition
/// </summary>
public class UpdatePipelineDefinitionRequest
{
    /// <summary>
    /// The definition to update
    /// </summary>
    [JsonPropertyName("definition")]
    public PipelineDefinition Definition { get; set; } = new();
}
