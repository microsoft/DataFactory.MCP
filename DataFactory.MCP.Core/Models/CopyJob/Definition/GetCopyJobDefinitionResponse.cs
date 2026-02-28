using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.CopyJob.Definition;

/// <summary>
/// Internal class for HTTP response deserialization when fetching copy job definitions
/// </summary>
internal sealed class GetCopyJobDefinitionResponse
{
    [JsonPropertyName("definition")]
    public CopyJobDefinition Definition { get; set; } = new();
}
