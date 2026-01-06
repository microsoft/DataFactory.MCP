using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Dataflow.Query;

/// <summary>
/// Execute Dataflow Query request payload
/// </summary>
public class ExecuteDataflowQueryRequest
{
    /// <summary>
    /// The name of the query to execute
    /// </summary>
    [JsonPropertyName("QueryName")]
    [Required(ErrorMessage = "Query name is required")]
    public string QueryName { get; set; } = string.Empty;

    /// <summary>
    /// The custom mashup document containing the M query logic
    /// </summary>
    [JsonPropertyName("customMashupDocument")]
    [Required(ErrorMessage = "Custom mashup document is required")]
    public string CustomMashupDocument { get; set; } = string.Empty;
}