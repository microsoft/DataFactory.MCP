using DataFactory.MCP.Models.Dataflow.Query;

namespace DataFactory.MCP.Extensions;

/// <summary>
/// Extensions for formatting dataflow query responses
/// </summary>
public static class ArrowDataExtensions
{
    public static object CreateArrowDataReport(this ExecuteDataflowQueryResponse response)
    {
        var data = response.Summary?.StructuredSampleData;
        var columns = data?.Keys.ToList() ?? new List<string>();
        var rowCount = data?.Values.FirstOrDefault()?.Count ?? 0;

        return new
        {
            table = new
            {
                format = "Table",
                rowCount = rowCount,
                columnCount = columns.Count,
                summary = $"{rowCount} rows × {columns.Count} columns",
                columns = columns.Select(col => new
                {
                    name = col,
                    dataType = InferDataType(data?.GetValueOrDefault(col) ?? new List<object>())
                }).ToArray(),
                rows = CreateRows(data, columns, rowCount)
            },
            executionSummary = new
            {
                success = response.Success,
                contentType = response.ContentType,
                contentLength = response.ContentLength,
                dataSize = FormatBytes(response.ContentLength),
                executionMetadata = response.Metadata
            }
        };
    }

    private static object[] CreateRows(Dictionary<string, List<object>>? data, List<string> columns, int rowCount)
    {
        if (data == null || rowCount == 0) return Array.Empty<object>();

        var rows = new object[rowCount];
        for (int i = 0; i < rowCount; i++)
        {
            var row = new Dictionary<string, object>();
            foreach (var col in columns)
            {
                var colData = data.GetValueOrDefault(col);
                row[col] = i < (colData?.Count ?? 0) ? colData![i]?.ToString() ?? "" : "";
            }
            rows[i] = row;
        }
        return rows;
    }

    private static string InferDataType(List<object> values)
    {
        var firstNonNull = values.FirstOrDefault(v => v != null);
        return firstNonNull?.GetType().Name switch
        {
            "Int32" => "Int32",
            "String" => "String",
            _ => "String"
        };
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.##} KB";
        return $"{bytes / (1024.0 * 1024):0.##} MB";
    }
}