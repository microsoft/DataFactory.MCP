using System.Text.Json.Serialization;

namespace DataFactory.WindowsMCP.Models.Dataflow.Excel;

/// <summary>
/// Execute Dataflow Query response
/// </summary>
public class AddQueryTableResponse
{
    /// <summary>
    /// Indicates if the query table addition was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the query table execution failed
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}