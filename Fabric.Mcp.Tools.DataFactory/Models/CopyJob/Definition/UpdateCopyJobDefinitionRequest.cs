using System.Text.Json.Serialization;

namespace Fabric.Mcp.Tools.DataFactory.Models.CopyJob.Definition;

/// <summary>
/// Request model for updating copy job definition
/// </summary>
public class UpdateCopyJobDefinitionRequest
{
    /// <summary>
    /// The definition to update
    /// </summary>
    [JsonPropertyName("definition")]
    public CopyJobDefinition Definition { get; set; } = new();
}
