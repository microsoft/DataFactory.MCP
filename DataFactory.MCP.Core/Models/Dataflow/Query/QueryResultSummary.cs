using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Dataflow.Query;

/// <summary>
/// Summary of query result data extracted from Apache Arrow format
/// </summary>
public class QueryResultSummary
{
    /// <summary>
    /// Detected column names in the result set
    /// </summary>
    [JsonPropertyName("columns")]
    public List<string>? Columns { get; set; }

    /// <summary>
    /// Number of rows in the result (if determinable)
    /// </summary>
    [JsonPropertyName("estimatedRowCount")]
    public int? EstimatedRowCount { get; set; }

    /// <summary>
    /// Structured sample data from Arrow format
    /// </summary>
    [JsonPropertyName("structuredSampleData")]
    public Dictionary<string, List<object>>? StructuredSampleData { get; set; }

    /// <summary>
    /// Number of Arrow record batches
    /// </summary>
    [JsonPropertyName("batchCount")]
    public int BatchCount { get; set; }

    /// <summary>
    /// Indicates if Arrow parsing was successful
    /// </summary>
    [JsonPropertyName("arrowParsingSuccess")]
    public bool ArrowParsingSuccess { get; set; }

    /// <summary>
    /// Arrow parsing error if any
    /// </summary>
    [JsonPropertyName("arrowParsingError")]
    public string? ArrowParsingError { get; set; }
}