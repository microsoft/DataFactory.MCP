using Apache.Arrow;
using Apache.Arrow.Ipc;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models.Arrow;
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
    /// Reads Apache Arrow stream and extracts metadata and formatted data
    /// </summary>
    /// <param name="arrowData">The Apache Arrow binary data</param>
    /// <returns>Formatted Arrow data information</returns>
    public Task<ArrowDataInfo> ReadArrowStreamAsync(byte[] arrowData)
    {
        return Task.Run(() => ReadArrowStream(arrowData));
    }

    /// <summary>
    /// Reads Apache Arrow stream and extracts metadata and formatted data (synchronous version)
    /// </summary>
    /// <param name="arrowData">The Apache Arrow binary data</param>
    /// <returns>Formatted Arrow data information</returns>
    private ArrowDataInfo ReadArrowStream(byte[] arrowData)
    {
        try
        {
            _logger.LogDebug("Starting Arrow stream processing for {DataSize} bytes", arrowData.Length);
            return ProcessArrowStream(arrowData);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Arrow parsing failed, creating minimal fallback info");
            return CreateFallbackInfo(ex);
        }
    }

    private ArrowDataInfo ProcessArrowStream(byte[] arrowData)
    {
        using var stream = new MemoryStream(arrowData);
        using var reader = new ArrowStreamReader(stream);

        var info = new ArrowDataInfo
        {
            Schema = reader.Schema != null ? ExtractSchemaInfo(reader.Schema) : null,
            Success = true
        };

        var batches = ReadAllBatches(reader, info);
        _logger.LogDebug("Read {BatchCount} batches with total {TotalRows} rows", batches.Count, info.TotalRows);

        info.AllData = ExtractAllData(batches, info.Schema?.Columns);

        info.BatchCount = batches.Count;
        return info;
    }

    private static List<RecordBatch> ReadAllBatches(ArrowStreamReader reader, ArrowDataInfo info)
    {
        var batches = new List<RecordBatch>();
        while (reader.ReadNextRecordBatch() is { } batch)
        {
            batches.Add(batch);
            info.TotalRows += batch.Length;
        }
        return batches;
    }

    private static ArrowSchemaInfo ExtractSchemaInfo(Schema schema) => new()
    {
        FieldCount = schema.FieldsList.Count,
        Columns = schema.FieldsList.Select(field => new ArrowColumnInfo
        {
            Name = field.Name,
            DataType = field.DataType.TypeId.ToString(),
            IsNullable = field.IsNullable,
            Metadata = field.Metadata?.Keys.ToDictionary(k => k, k => field.Metadata[k]) ?? []
        }).ToList()
    };

    private static Dictionary<string, List<object>> ExtractAllData(List<RecordBatch> batches, List<ArrowColumnInfo>? columns)
    {
        if (columns == null || columns.Count == 0)
            return [];

        var allData = columns.ToDictionary(col => col.Name, _ => new List<object>());

        foreach (var batch in batches)
        {
            ExtractBatchData(batch, columns, allData, batch.Length);
        }

        return allData;
    }

    private static void ExtractBatchData(RecordBatch batch, List<ArrowColumnInfo> columns, Dictionary<string, List<object>> data, int rowLimit)
    {
        for (int colIndex = 0; colIndex < Math.Min(batch.ColumnCount, columns.Count); colIndex++)
        {
            var column = batch.Column(colIndex);
            var columnName = columns[colIndex].Name;

            for (int rowIndex = 0; rowIndex < Math.Min(rowLimit, column.Length); rowIndex++)
            {
                try
                {
                    var value = ExtractValueFromArray(column, rowIndex);
                    data[columnName].Add(value ?? "");
                }
                catch
                {
                    data[columnName].Add("");
                }
            }
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

    private static ArrowDataInfo CreateFallbackInfo(Exception? ex = null)
    {
        return new ArrowDataInfo
        {
            Success = false,
            Error = ex?.Message ?? "Unknown error during Arrow parsing",
            Schema = new ArrowSchemaInfo
            {
                FieldCount = 0,
                Columns = []
            },
            AllData = [],
            TotalRows = 0,
            BatchCount = 0
        };
    }
}