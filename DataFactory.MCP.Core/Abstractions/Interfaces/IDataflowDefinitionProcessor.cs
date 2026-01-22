using DataFactory.MCP.Models.Connection;
using DataFactory.MCP.Models.Dataflow;
using DataFactory.MCP.Models.Dataflow.Definition;
using System.Text.Json;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Service for processing and transforming dataflow definitions
/// </summary>
public interface IDataflowDefinitionProcessor
{
    /// <summary>
    /// Decodes a raw dataflow definition into a structured format
    /// </summary>
    /// <param name="rawDefinition">The raw dataflow definition from API</param>
    /// <returns>Decoded dataflow definition with readable content</returns>
    DecodedDataflowDefinition DecodeDefinition(DataflowDefinition rawDefinition);

    /// <summary>
    /// Adds one or more connections to an existing dataflow definition
    /// </summary>
    /// <param name="definition">The dataflow definition to modify</param>
    /// <param name="connections">List of tuples containing (connection, connectionId, clusterId) for each connection to add</param>
    /// <returns>Updated dataflow definition</returns>
    DataflowDefinition AddConnectionsToDefinition(
        DataflowDefinition definition,
        IEnumerable<(Connection Connection, string ConnectionId, string? ClusterId)> connections);

    /// <summary>
    /// Adds or updates a query in an existing dataflow definition
    /// </summary>
    /// <param name="definition">The dataflow definition to modify</param>
    /// <param name="queryName">The name of the query to add or update</param>
    /// <param name="mCode">The M (Power Query) code for the query</param>
    /// <param name="attribute">Optional attribute for the query (e.g., [DataDestinations = ...])</param>
    /// <param name="sectionAttribute">Optional section-level attribute (e.g., [StagingDefinition = [Kind = "FastCopy"]])</param>
    /// <returns>Updated dataflow definition</returns>
    DataflowDefinition AddOrUpdateQueryInDefinition(
        DataflowDefinition definition,
        string queryName,
        string mCode,
        string? attribute = null,
        string? sectionAttribute = null);

    /// <summary>
    /// Syncs the dataflow definition with a new M document by replacing the entire mashup
    /// and updating the query metadata to match.
    /// This is a declarative approach: the provided document IS the desired state.
    /// </summary>
    /// <param name="definition">The current dataflow definition</param>
    /// <param name="newMashupDocument">The complete M section document to sync</param>
    /// <param name="parsedQueries">List of parsed queries with their names, code, and attributes</param>
    /// <returns>Updated dataflow definition with synced mashup and metadata</returns>
    DataflowDefinition SyncMashupInDefinition(
        DataflowDefinition definition,
        string newMashupDocument,
        IList<(string QueryName, string MCode, string? Attribute)> parsedQueries);
}