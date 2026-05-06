using System.Text.Json.Serialization;

namespace Fabric.Mcp.Tools.DataFactory.Models.Pipeline.Definition;

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
