using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Pipeline.Definition;

/// <summary>
/// Internal class for HTTP response deserialization when fetching pipeline definitions
/// </summary>
internal sealed class GetPipelineDefinitionResponse
{
    [JsonPropertyName("definition")]
    public PipelineDefinition Definition { get; set; } = new();
}
