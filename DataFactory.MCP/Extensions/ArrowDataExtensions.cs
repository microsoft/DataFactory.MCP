using System.Text.Json;
using DataFactory.MCP.Models.Dataflow;

namespace DataFactory.MCP.Extensions;

/// <summary>
/// Extensions for formatting Apache Arrow data responses
/// </summary>
public static class ArrowDataExtensions
{
    /// <summary>
    /// Formats Apache Arrow data summary for display with full data
    /// </summary>
    /// <param name="summary">The query result summary</param>
    public static object ToFormattedArrowSummary(this QueryResultSummary summary)
    {
        var formatted = new
        {
            DataFormat = "Apache Arrow Stream",
            ParsingStatus = summary.ArrowParsingSuccess ? "Success" : "Fallback to text parsing",
            ParsingError = summary.ArrowParsingError,

            Schema = summary.ArrowSchema != null ? new
            {
                FieldCount = summary.ArrowSchema.FieldCount,
                Columns = summary.ArrowSchema.Columns?.Select(c => new
                {
                    Name = c.Name,
                    DataType = c.DataType,
                    IsNullable = c.IsNullable,
                    HasMetadata = c.Metadata?.Any() == true
                })
            } : null,

            DataSummary = new
            {
                TotalRows = summary.EstimatedRowCount,
                BatchCount = summary.BatchCount,
                Columns = summary.Columns,
                FullDataAvailable = summary.SampleData?.Keys.ToList()
            },

            Table = summary.StructuredSampleData != null
                ? FormatAsTable(summary.StructuredSampleData)
                : CreateEmptyTable(),

            Format = summary.Format,

            ProcessingNotes = new[]
            {
                summary.ArrowParsingSuccess
                    ? "Data successfully parsed using Apache Arrow libraries"
                    : "Arrow parsing failed, used text-based extraction as fallback",

                $"Extracted {summary.Columns?.Count ?? 0} columns with {summary.EstimatedRowCount ?? 0} estimated rows",

                summary.BatchCount > 0
                    ? $"Data organized in {summary.BatchCount} Arrow record batches"
                    : "Batch information not available"
            }
        };

        return formatted;
    }

    private static object FormatAsTable(Dictionary<string, List<object>> structuredData)
    {
        if (!structuredData.Any())
            return CreateEmptyTable();

        // Get all column names (exclude PQ Arrow Metadata)
        var columns = structuredData.Keys.Where(k => k != "PQ Arrow Metadata").ToList();

        // Get the number of rows (assume all columns have the same number of rows)
        var rowCount = structuredData.Values.FirstOrDefault()?.Count ?? 0;

        // Create column definitions
        var columnDefinitions = columns.Select(col => new
        {
            Name = col,
            DataType = InferDataType(structuredData.ContainsKey(col) ? structuredData[col] : new List<object>()),
            Width = Math.Max(col.Length, GetMaxValueLength(structuredData.ContainsKey(col) ? structuredData[col] : new List<object>()))
        }).ToList();

        // Create table rows
        var rows = new List<Dictionary<string, object>>();
        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            var row = new Dictionary<string, object>();
            foreach (var col in columns)
            {
                if (structuredData.ContainsKey(col) && rowIndex < structuredData[col].Count)
                {
                    row[col] = structuredData[col][rowIndex]?.ToString() ?? "";
                }
                else
                {
                    row[col] = "";
                }
            }
            rows.Add(row);
        }

        // Generate Markdown table for visual rendering
        var markdownTable = GenerateMarkdownTable(columns, rows);

        return new
        {
            Format = "Table",
            RowCount = rowCount,
            ColumnCount = columns.Count,
            Summary = $"{rowCount} rows × {columns.Count} columns",
            MarkdownTable = markdownTable,
            Columns = columnDefinitions,
            Rows = rows
        };
    }

    private static object CreateEmptyTable()
    {
        return new
        {
            Format = "Table",
            RowCount = 0,
            ColumnCount = 0,
            Columns = Array.Empty<object>(),
            Rows = Array.Empty<object>(),
            Summary = "No data available"
        };
    }

    private static int GetMaxValueLength(List<object> values)
    {
        if (!values.Any()) return 10;

        var maxLength = values
            .Select(v => v?.ToString()?.Length ?? 0)
            .DefaultIfEmpty(0)
            .Max();

        return Math.Min(Math.Max(maxLength, 10), 50); // Min 10, Max 50 characters
    }

    private static string GenerateMarkdownTable(List<string> columns, List<Dictionary<string, object>> rows)
    {
        if (!columns.Any() || !rows.Any())
            return "| No Data |\n|---------|";

        var sb = new System.Text.StringBuilder();

        // Create header row
        sb.Append("| ");
        sb.Append(string.Join(" | ", columns.Select(col => EscapeMarkdown(col))));
        sb.AppendLine(" |");

        // Create separator row
        sb.Append("| ");
        sb.Append(string.Join(" | ", columns.Select(_ => "---")));
        sb.AppendLine(" |");

        // Create data rows
        foreach (var row in rows)
        {
            sb.Append("| ");
            var cellValues = columns.Select(col =>
            {
                var value = row.ContainsKey(col) ? row[col]?.ToString() ?? "" : "";
                // Truncate long values for better display
                if (value.Length > 100)
                    value = value.Substring(0, 97) + "...";
                return EscapeMarkdown(value);
            });
            sb.Append(string.Join(" | ", cellValues));
            sb.AppendLine(" |");
        }

        return sb.ToString();
    }

    private static string EscapeMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        // Escape markdown special characters and replace newlines
        return text
            .Replace("|", "\\|")
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("  ", " ")
            .Trim();
    }

    private static string InferDataType(List<object> values)
    {
        if (!values.Any()) return "Unknown";

        var firstNonNull = values.FirstOrDefault(v => v != null);
        return firstNonNull?.GetType().Name ?? "Null";
    }

    /// <summary>
    /// Creates a comprehensive Arrow data report with full data as visual table
    /// </summary>
    /// <param name="response">The execution response</param>
    public static object CreateArrowDataReport(this ExecuteDataflowQueryResponse response)
    {
        var arrowSummary = response.Summary?.ToFormattedArrowSummary();

        // Extract the markdown table if available
        string markdownTable = "";
        if (arrowSummary is { } summary)
        {
            var summaryType = summary.GetType();
            var tableProperty = summaryType.GetProperty("Table");
            if (tableProperty?.GetValue(summary) is { } tableObj)
            {
                var tableType = tableObj.GetType();
                var markdownProperty = tableType.GetProperty("MarkdownTable");
                markdownTable = markdownProperty?.GetValue(tableObj)?.ToString() ?? "";
            }
        }

        return new
        {
            // Put the visual table first for immediate display
            VisualTable = !string.IsNullOrEmpty(markdownTable) ? markdownTable : "No data available",

            ExecutionSummary = new
            {
                Success = response.Success,
                ContentType = response.ContentType,
                ContentLength = response.ContentLength,
                DataSize = FormatBytes(response.ContentLength),
                ExecutionMetadata = response.Metadata
            },

            ArrowData = arrowSummary,

            UsageGuidelines = new
            {
                RecommendedTools = new[]
                {
                    "Apache Arrow libraries (.NET, Python, Java, etc.)",
                    "Power BI for business intelligence",
                    "Apache Spark for large-scale data processing",
                    "Pandas with PyArrow for Python data analysis",
                    "DuckDB for SQL queries on Arrow data"
                },

                ExampleUsage = new
                {
                    DotNet = "Use Apache.Arrow NuGet package to read the binary data",
                    Python = "Use pyarrow.ipc.open_stream() to read the Arrow stream",
                    PowerBI = "Import directly as Arrow data source",
                    SQL = "Use DuckDB to query Arrow data with standard SQL"
                }
            },

            TechnicalDetails = new
            {
                Format = "Apache Arrow Columnar Format",
                Specification = "https://arrow.apache.org/docs/format/Columnar.html",
                Benefits = new[]
                {
                    "Efficient columnar storage format",
                    "Zero-copy data sharing between systems",
                    "Language-agnostic data exchange",
                    "Optimized for analytics workloads"
                }
            }
        };
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}