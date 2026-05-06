using System.Text.Json.Serialization;

namespace Fabric.Mcp.Tools.DataFactory.Models.Dataflow.Definition;

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
