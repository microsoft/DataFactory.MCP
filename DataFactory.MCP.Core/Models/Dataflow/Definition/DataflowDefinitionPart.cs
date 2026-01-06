using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Dataflow.Definition;

/// <summary>
/// Dataflow definition part object
/// </summary>
public class DataflowDefinitionPart
{
    /// <summary>
    /// The Dataflow public definition part path
    /// </summary>
    [JsonPropertyName("path")]
    [Required(ErrorMessage = "Path is required")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// The Dataflow public definition part payload
    /// </summary>
    [JsonPropertyName("payload")]
    [Required(ErrorMessage = "Payload is required")]
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// The payload type
    /// </summary>
    [JsonPropertyName("payloadType")]
    public PayloadType PayloadType { get; set; } = PayloadType.InlineBase64;
}
