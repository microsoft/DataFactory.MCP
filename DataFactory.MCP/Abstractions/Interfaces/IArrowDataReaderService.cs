using DataFactory.MCP.Models.Arrow;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Service for reading and parsing Apache Arrow data streams
/// </summary>
public interface IArrowDataReaderService
{
    /// <summary>
    /// Reads Apache Arrow stream and extracts metadata and formatted data
    /// </summary>
    /// <param name="arrowData">The Apache Arrow binary data</param>
    /// <returns>Formatted Arrow data information</returns>
    Task<ArrowDataInfo> ReadArrowStreamAsync(byte[] arrowData);
}