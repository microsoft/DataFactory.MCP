using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Dataflow;

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
    /// Sample values extracted from the data (limited for display)
    /// </summary>
    [JsonPropertyName("sampleData")]
    public Dictionary<string, List<string>>? SampleData { get; set; }

    /// <summary>
    /// Number of rows in the result (if determinable)
    /// </summary>
    [JsonPropertyName("estimatedRowCount")]
    public int? EstimatedRowCount { get; set; }

    /// <summary>
    /// Format description
    /// </summary>
    [JsonPropertyName("format")]
    public string Format { get; set; } = "Apache Arrow";

    /// <summary>
    /// Enhanced Arrow schema information
    /// </summary>
    [JsonPropertyName("arrowSchema")]
    public ArrowSchemaDetails? ArrowSchema { get; set; }

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

/// <summary>
/// Detailed Arrow schema information
/// </summary>
public class ArrowSchemaDetails
{
    /// <summary>
    /// Number of fields in the schema
    /// </summary>
    [JsonPropertyName("fieldCount")]
    public int FieldCount { get; set; }

    /// <summary>
    /// Column information
    /// </summary>
    [JsonPropertyName("columns")]
    public List<ArrowColumnDetails>? Columns { get; set; }
}

/// <summary>
/// Detailed information about an Arrow column
/// </summary>
public class ArrowColumnDetails
{
    /// <summary>
    /// Column name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Column data type
    /// </summary>
    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Whether the column is nullable
    /// </summary>
    [JsonPropertyName("isNullable")]
    public bool IsNullable { get; set; }

    /// <summary>
    /// Column metadata
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}