using System.Text.Json;

namespace DataFactory.MCP.Models.Dataflow.Definition;

/// <summary>
/// Decoded dataflow definition for easier access
/// </summary>
public class MetadataPatch
{
    /// <summary>
    /// indicate whether the query is load enabled or not.
    /// </summary>
    public bool? LoadEnabled { get; set; }

    /// <summary>
    /// indicate whether the query is hidden or not
    /// </summary>
    public bool? IsHidden { get; set; }

    /// <summary>
    /// Decoded mashup.pq content (Power Query M code)
    /// </summary>
    public DestinationSettings? DestinationSettings { get; set; }
}
