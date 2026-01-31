using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataFactory.WindowsMCP.Models.Dataflow.Excel;

/// <summary>
/// Execute Dataflow Query request payload
/// </summary>
public class AddQueryTableRequest
{
    /// <summary>
    /// The mashup query to add as a query table
    /// </summary>
    [JsonPropertyName("mashupQuery")]
    [Required(ErrorMessage = "Mashup query is required")]
    public string MashupQuery { get; set; } = string.Empty;

    /// <summary>
    /// The initial data to populate the query table (optional)
    /// </summary>
    [JsonPropertyName("jsonData")]
    public string JsonData { get; set; } = string.Empty;
}