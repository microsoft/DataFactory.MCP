using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Dataflow.Definition;

/// <summary>
/// The type of the definition part payload
/// </summary>
public enum PayloadType
{
    /// <summary>
    /// Inline Base 64
    /// </summary>
    [JsonPropertyName("InlineBase64")]
    InlineBase64
}
