using DataFactory.MCP.Models.Dataflow.Query;

namespace DataFactory.MCP.Extensions;

/// <summary>
/// Extensions for formatting Apache Arrow data responses
/// </summary>
public static class ArrowDataExtensions
{
    /// <summary>
    /// Creates a comprehensive Arrow data report with structured data
    /// </summary>
    /// <param name="response">The execution response</param>
    public static object CreateArrowDataReport(this ExecuteDataflowQueryResponse response) => new
    {
        Table = response.Summary?.StructuredSampleData != null
            ? FormatAsTable(response.Summary.StructuredSampleData)
            : CreateEmptyTable(),
        ExecutionSummary = CreateExecutionSummary(response)
    };

    private static object FormatAsTable(Dictionary<string, List<object>> structuredData)
    {
        if (structuredData.Count == 0) return CreateEmptyTable();

        var columns = structuredData.Keys.Where(k => k != "PQ Arrow Metadata").ToList();
        var rowCount = structuredData.Values.FirstOrDefault()?.Count ?? 0;

        return new
        {
            Format = "Table",
            RowCount = rowCount,
            ColumnCount = columns.Count,
            Summary = $"{rowCount} rows × {columns.Count} columns",
            Columns = CreateColumnDefinitions(structuredData, columns),
            Rows = CreateTableRows(structuredData, columns, rowCount)
        };
    }

    private static object CreateEmptyTable() => new
    {
        Format = "Table",
        RowCount = 0,
        ColumnCount = 0,
        Columns = Array.Empty<object>(),
        Rows = Array.Empty<object>(),
        Summary = "No data available"
    };

    private static List<object> CreateColumnDefinitions(Dictionary<string, List<object>> data, List<string> columns) =>
        columns.Select(col => new
        {
            Name = col,
            DataType = InferDataType(data.TryGetValue(col, out var values) ? values : [])
        }).Cast<object>().ToList();

    private static List<Dictionary<string, object>> CreateTableRows(Dictionary<string, List<object>> data, List<string> columns, int rowCount)
    {
        var rows = new List<Dictionary<string, object>>();

        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            var row = new Dictionary<string, object>();
            foreach (var col in columns)
            {
                row[col] = data.TryGetValue(col, out var colData) && rowIndex < colData.Count
                    ? colData[rowIndex]?.ToString() ?? ""
                    : "";
            }
            rows.Add(row);
        }

        return rows;
    }

    private static string InferDataType(List<object> values) =>
        values.FirstOrDefault(v => v != null)?.GetType().Name ?? (values.Count == 0 ? "Unknown" : "Null");

    private static object CreateExecutionSummary(ExecuteDataflowQueryResponse response) => new
    {
        Success = response.Success,
        ContentType = response.ContentType,
        ContentLength = response.ContentLength,
        DataSize = FormatBytes(response.ContentLength),
        ExecutionMetadata = response.Metadata
    };

    private static readonly string[] SizeUnits = { "B", "KB", "MB", "GB" };

    private static string FormatBytes(long bytes)
    {
        double size = bytes;
        int unit = 0;

        while (size >= 1024 && unit < SizeUnits.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.##} {SizeUnits[unit]}";
    }
}