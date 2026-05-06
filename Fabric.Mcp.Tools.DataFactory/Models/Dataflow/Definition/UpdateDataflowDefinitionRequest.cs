using System.Text.Json.Serialization;

namespace Fabric.Mcp.Tools.DataFactory.Models.Dataflow.Definition;

/// <summary>
/// Request model for updating dataflow definition
/// </summary>
public class UpdateDataflowDefinitionRequest
{
    /// <summary>
    /// The definition to update
    /// </summary>
    [JsonPropertyName("definition")]
    public DataflowDefinition Definition { get; set; } = new();
}
