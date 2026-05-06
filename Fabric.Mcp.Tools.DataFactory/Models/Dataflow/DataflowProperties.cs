using System.Text.Json.Serialization;

namespace Fabric.Mcp.Tools.DataFactory.Models.Dataflow;

/// <summary>
/// Represents additional properties of a dataflow
/// </summary>
public class DataflowProperties
{
    /// <summary>
    /// Indicates if the dataflow is parametric
    /// </summary>
    [JsonPropertyName("isParametric")]
    public bool IsParametric { get; set; }
}
