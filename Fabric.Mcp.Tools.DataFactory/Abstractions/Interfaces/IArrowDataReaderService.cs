using Fabric.Mcp.Tools.DataFactory.Models.Dataflow.Query;

namespace Fabric.Mcp.Tools.DataFactory.Abstractions.Interfaces;

/// <summary>
/// Service for reading and parsing Apache Arrow data streams
/// </summary>
public interface IArrowDataReaderService
{
    /// <summary>
    /// Reads Apache Arrow stream and creates query result summary directly
    /// </summary>
    /// <param name="arrowData">The Apache Arrow binary data</param>
    /// <returns>Query result summary for dataflow responses</returns>
    Task<QueryResultSummary> ReadArrowStreamAsync(byte[] arrowData);
}