using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Dataflow.Definition;

/// <summary>
/// Internal class for HTTP response deserialization when fetching dataflow definitions
/// </summary>
internal sealed class GetDataflowDefinitionHttpResponse
{
    [JsonPropertyName("definition")]
    public DataflowDefinition Definition { get; set; } = new();
}
