using System.Text.Json;

namespace DataFactory.MCP.Models.Dataflow.Definition;

/// <summary>
/// Decoded dataflow definition for easier access
/// </summary>
public class DecodedDataflowDefinition
{
    /// <summary>
    /// Decoded queryMetadata.json content as structured object
    /// </summary>
    public JsonElement? QueryMetadata { get; set; }

    /// <summary>
    /// Decoded mashup.pq content (Power Query M code)
    /// </summary>
    public string? MashupQuery { get; set; }

    /// <summary>
    /// Decoded .platform content as structured object
    /// </summary>
    public JsonElement? PlatformMetadata { get; set; }

    /// <summary>
    /// Raw definition parts (Base64 encoded)
    /// </summary>
    public List<DataflowDefinitionPart> RawParts { get; set; } = new();
}
