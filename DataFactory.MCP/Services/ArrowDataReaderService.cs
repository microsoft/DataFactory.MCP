using Apache.Arrow;
using Apache.Arrow.Ipc;
using System.Text;
using System.Text.Json;
using DataFactory.MCP.Models.Dataflow;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for reading and formatting Apache Arrow data streams
/// </summary>
public class ArrowDataReaderService
{
    /// <summary>
    /// Reads Apache Arrow stream and extracts metadata and formatted data
    /// </summary>
    /// <param name="arrowData">The Apache Arrow binary data</param>
    /// <param name="returnAllData">If true, returns all data; if false, returns sample data only</param>
    /// <returns>Formatted Arrow data information</returns>
    public static ArrowDataInfo ReadArrowStream(byte[] arrowData, bool returnAllData = false)
    {
        var info = new ArrowDataInfo();

        try
        {
            using var stream = new MemoryStream(arrowData);
            using var reader = new ArrowStreamReader(stream);

            // Read schema first
            var schema = reader.Schema;
            if (schema != null)
            {
                info.Schema = ExtractSchemaInfo(schema);
            }

            // Read all record batches
            var allRecords = new List<RecordBatch>();
            RecordBatch? recordBatch;

            while ((recordBatch = reader.ReadNextRecordBatch()) != null)
            {
                allRecords.Add(recordBatch);
                info.TotalRows += recordBatch.Length;
            }

            // Extract data based on returnAllData parameter
            if (returnAllData)
            {
                info.AllData = ExtractAllData(allRecords, info.Schema?.Columns);
            }
            else
            {
                info.SampleData = ExtractSampleData(allRecords.Take(3), info.Schema?.Columns);
            }

            info.BatchCount = allRecords.Count;
            info.Success = true;
        }
        catch (Exception ex)
        {
            // Fallback to basic text extraction
            info = ExtractBasicInfo(arrowData, ex);
        }

        return info;
    }

    private static ArrowSchemaInfo ExtractSchemaInfo(Schema schema)
    {
        var schemaInfo = new ArrowSchemaInfo
        {
            FieldCount = schema.FieldsList.Count,
            Columns = new List<ArrowColumnInfo>()
        };

        foreach (var field in schema.FieldsList)
        {
            schemaInfo.Columns.Add(new ArrowColumnInfo
            {
                Name = field.Name,
                DataType = field.DataType.TypeId.ToString(),
                IsNullable = field.IsNullable,
                Metadata = field.Metadata?.Keys.ToDictionary(k => k, k => field.Metadata[k]) ?? new Dictionary<string, string>()
            });
        }

        return schemaInfo;
    }

    private static Dictionary<string, List<object>> ExtractAllData(IEnumerable<RecordBatch> batches, List<ArrowColumnInfo>? columns)
    {
        var allData = new Dictionary<string, List<object>>();

        if (columns == null) return allData;

        foreach (var column in columns)
        {
            allData[column.Name] = new List<object>();
        }

        foreach (var batch in batches)
        {
            for (int colIndex = 0; colIndex < batch.ColumnCount && colIndex < columns.Count; colIndex++)
            {
                var column = batch.Column(colIndex);
                var columnName = columns[colIndex].Name;

                // Extract ALL values from this batch
                for (int rowIndex = 0; rowIndex < column.Length; rowIndex++)
                {
                    try
                    {
                        var value = ExtractValueFromArray(column, rowIndex);
                        allData[columnName].Add(value ?? "");
                    }
                    catch
                    {
                        allData[columnName].Add(""); // Add empty string for problematic values
                    }
                }
            }
        }

        return allData;
    }

    private static Dictionary<string, List<object>> ExtractSampleData(IEnumerable<RecordBatch> batches, List<ArrowColumnInfo>? columns)
    {
        var sampleData = new Dictionary<string, List<object>>();

        if (columns == null) return sampleData;

        foreach (var column in columns)
        {
            sampleData[column.Name] = new List<object>();
        }

        foreach (var batch in batches)
        {
            for (int colIndex = 0; colIndex < batch.ColumnCount && colIndex < columns.Count; colIndex++)
            {
                var column = batch.Column(colIndex);
                var columnName = columns[colIndex].Name;
                var values = new List<object>();

                // Extract up to 5 sample values per batch
                var sampleCount = Math.Min(5, column.Length);
                for (int rowIndex = 0; rowIndex < sampleCount; rowIndex++)
                {
                    try
                    {
                        var value = ExtractValueFromArray(column, rowIndex);
                        if (value != null)
                        {
                            values.Add(value);
                        }
                    }
                    catch
                    {
                        // Skip problematic values
                    }
                }

                sampleData[columnName].AddRange(values);

                // Limit total samples per column
                if (sampleData[columnName].Count > 10)
                {
                    sampleData[columnName] = sampleData[columnName].Take(10).ToList();
                }
            }
        }

        return sampleData;
    }

    private static object? ExtractValueFromArray(IArrowArray array, int index)
    {
        if (array.IsNull(index)) return null;

        return array switch
        {
            StringArray stringArray => stringArray.GetString(index),
            Int32Array int32Array => int32Array.GetValue(index),
            Int64Array int64Array => int64Array.GetValue(index),
            DoubleArray doubleArray => doubleArray.GetValue(index),
            BooleanArray boolArray => boolArray.GetValue(index),
            TimestampArray timestampArray => timestampArray.GetTimestamp(index)?.ToString("yyyy-MM-dd HH:mm:ss"),
            Decimal128Array decimal128Array => decimal128Array.GetValue(index)?.ToString(),
            FloatArray floatArray => floatArray.GetValue(index),
            _ => $"[{array.GetType().Name}] - value at index {index}"
        };
    }

    private static ArrowDataInfo ExtractBasicInfo(byte[] arrowData, Exception? ex = null)
    {
        var info = new ArrowDataInfo
        {
            Success = false,
            Error = ex?.Message,
            Schema = new ArrowSchemaInfo { Columns = new List<ArrowColumnInfo>() },
            SampleData = new Dictionary<string, List<object>>()
        };

        try
        {
            // Fallback to text-based extraction as before
            var stringContent = Encoding.UTF8.GetString(arrowData);

            // Look for common column patterns
            var detectedColumns = new List<string>();
            var commonColumns = new[] { "RoleInstance", "ProcessName", "Message", "Timestamp", "Level", "Id" };

            foreach (var column in commonColumns)
            {
                if (stringContent.Contains(column))
                {
                    detectedColumns.Add(column);
                    info.Schema.Columns.Add(new ArrowColumnInfo
                    {
                        Name = column,
                        DataType = "String",
                        IsNullable = true
                    });
                }
            }

            // Extract sample data using regex patterns
            ExtractSampleDataFromText(stringContent, info.SampleData);

            info.Schema.FieldCount = detectedColumns.Count;
            info.TotalRows = EstimateRowCount(stringContent);
        }
        catch (Exception extractEx)
        {
            info.Error = $"Arrow parsing failed: {ex?.Message}. Text extraction failed: {extractEx.Message}";
        }

        return info;
    }

    private static void ExtractSampleDataFromText(string content, Dictionary<string, List<object>> sampleData)
    {
        // Extract RoleInstance values
        var rolePattern = @"vmback_\d+";
        var roleMatches = System.Text.RegularExpressions.Regex.Matches(content, rolePattern);
        if (roleMatches.Count > 0)
        {
            sampleData["RoleInstance"] = roleMatches.Cast<System.Text.RegularExpressions.Match>()
                .Select(m => (object)m.Value).Distinct().Take(5).ToList();
        }

        // Extract ProcessName values
        var servicePattern = @"Microsoft\.Mashup\.Web\.[A-Za-z.]+";
        var serviceMatches = System.Text.RegularExpressions.Regex.Matches(content, servicePattern);
        if (serviceMatches.Count > 0)
        {
            sampleData["ProcessName"] = serviceMatches.Cast<System.Text.RegularExpressions.Match>()
                .Select(m => (object)m.Value).Distinct().Take(5).ToList();
        }

        // Extract common error messages
        var errorMessages = new[] {
            "Invalid workload hostname",
            "DataSource requested unhandled application property",
            "A generic MashupException was caught",
            "Unable to create a provider context"
        };

        var foundMessages = errorMessages.Where(msg => content.Contains(msg)).Take(5).ToList();
        if (foundMessages.Any())
        {
            sampleData["Message"] = foundMessages.Cast<object>().ToList();
        }
    }

    private static int EstimateRowCount(string content)
    {
        // Simple heuristic based on common patterns
        var rolePattern = @"vmback_\d+";
        var roleMatches = System.Text.RegularExpressions.Regex.Matches(content, rolePattern);
        return Math.Min(roleMatches.Count, 1000); // Cap at reasonable number
    }
}

/// <summary>
/// Information extracted from Apache Arrow data
/// </summary>
public class ArrowDataInfo
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public ArrowSchemaInfo? Schema { get; set; }
    public Dictionary<string, List<object>>? SampleData { get; set; }
    public Dictionary<string, List<object>>? AllData { get; set; }
    public int TotalRows { get; set; }
    public int BatchCount { get; set; }
}

/// <summary>
/// Apache Arrow schema information
/// </summary>
public class ArrowSchemaInfo
{
    public int FieldCount { get; set; }
    public List<ArrowColumnInfo>? Columns { get; set; }
}

/// <summary>
/// Information about an Arrow column
/// </summary>
public class ArrowColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}