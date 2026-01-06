using Apache.Arrow;
using Apache.Arrow.Ipc;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models.Dataflow.Query;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for reading and parsing Apache Arrow data streams
/// </summary>
public class ArrowDataReaderService : IArrowDataReaderService
{
    private readonly ILogger<ArrowDataReaderService> _logger;

    public ArrowDataReaderService(ILogger<ArrowDataReaderService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Reads Apache Arrow stream and creates query result summary directly
    /// </summary>
    public async Task<QueryResultSummary> ReadArrowStreamAsync(byte[] arrowData)
    {
        try
        {
            using var stream = new MemoryStream(arrowData);
            using var reader = new ArrowStreamReader(stream);

            var columns = reader.Schema?.FieldsList.Select(f => f.Name).ToList() ?? new List<string>();
            var allData = new Dictionary<string, List<object>>();
            var totalRows = 0;
            var batchCount = 0;

            // Initialize data structure
            foreach (var col in columns)
                allData[col] = new List<object>();

            // Read all batches
            while (reader.ReadNextRecordBatch() is { } batch)
            {
                batchCount++;
                totalRows += batch.Length;

                for (int colIndex = 0; colIndex < Math.Min(batch.ColumnCount, columns.Count); colIndex++)
                {
                    var column = batch.Column(colIndex);
                    var columnName = columns[colIndex];

                    for (int rowIndex = 0; rowIndex < batch.Length; rowIndex++)
                    {
                        try
                        {
                            var value = ExtractValueFromArray(column, rowIndex);
                            allData[columnName].Add(value ?? "");
                        }
                        catch
                        {
                            allData[columnName].Add("");
                        }
                    }
                }
            }

            return new QueryResultSummary
            {
                ArrowParsingSuccess = true,
                Columns = columns,
                EstimatedRowCount = totalRows,
                BatchCount = batchCount,
                StructuredSampleData = allData
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Arrow parsing failed");
            return new QueryResultSummary
            {
                ArrowParsingSuccess = false,
                ArrowParsingError = ex.Message,
                Columns = new List<string>(),
                EstimatedRowCount = 0,
                BatchCount = 0,
                StructuredSampleData = new Dictionary<string, List<object>>()
            };
        }
    }

    private static object? ExtractValueFromArray(IArrowArray array, int index) =>
        array.IsNull(index) ? null : array switch
        {
            StringArray str => str.GetString(index),
            Int32Array i32 => i32.GetValue(index),
            Int64Array i64 => i64.GetValue(index),
            DoubleArray dbl => dbl.GetValue(index),
            BooleanArray bln => bln.GetValue(index),
            TimestampArray ts => ts.GetTimestamp(index)?.ToString("yyyy-MM-dd HH:mm:ss"),
            Date32Array dt32 => DateTimeOffset.FromUnixTimeSeconds(dt32.GetValue(index) ?? 0).ToString("yyyy-MM-dd"),
            Date64Array dt64 => DateTimeOffset.FromUnixTimeMilliseconds(dt64.GetValue(index) ?? 0).ToString("yyyy-MM-dd"),
            Decimal128Array dec => dec.GetValue(index)?.ToString(),
            Decimal256Array dec256 => dec256.GetValue(index)?.ToString(),
            FloatArray flt => flt.GetValue(index),
            Int8Array i8 => i8.GetValue(index),
            Int16Array i16 => i16.GetValue(index),
            UInt8Array ui8 => ui8.GetValue(index),
            UInt16Array ui16 => ui16.GetValue(index),
            UInt32Array ui32 => ui32.GetValue(index),
            UInt64Array ui64 => ui64.GetValue(index),
            BinaryArray bin => Convert.ToBase64String(bin.GetBytes(index).ToArray()),
            _ => $"[{array.GetType().Name}] - Unsupported type"
        };


}