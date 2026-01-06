using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Dataflow.Query;

/// <summary>
/// Execute Dataflow Query response
/// </summary>
public class ExecuteDataflowQueryResponse
{
    /// <summary>
    /// The raw Apache Arrow binary data response
    /// </summary>
    [JsonPropertyName("data")]
    public byte[]? Data { get; set; }

    /// <summary>
    /// The content type of the response
    /// </summary>
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    /// <summary>
    /// The size of the response in bytes
    /// </summary>
    [JsonPropertyName("contentLength")]
    public long ContentLength { get; set; }

    /// <summary>
    /// Indicates if the query execution was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the execution failed
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Additional metadata about the query execution
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Summary of extracted readable content from the Arrow data (for display purposes)
    /// </summary>
    [JsonPropertyName("summary")]
    public QueryResultSummary? Summary { get; set; }
}