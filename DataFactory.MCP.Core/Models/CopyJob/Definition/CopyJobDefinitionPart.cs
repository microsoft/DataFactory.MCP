using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.CopyJob.Definition;

/// <summary>
/// Copy Job definition part object
/// </summary>
public class CopyJobDefinitionPart
{
    /// <summary>
    /// The Copy Job public definition part path
    /// </summary>
    [JsonPropertyName("path")]
    [Required(ErrorMessage = "Path is required")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// The Copy Job public definition part payload (base64 encoded)
    /// </summary>
    [JsonPropertyName("payload")]
    [Required(ErrorMessage = "Payload is required")]
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// The payload type
    /// </summary>
    [JsonPropertyName("payloadType")]
    public string PayloadType { get; set; } = "InlineBase64";
}
