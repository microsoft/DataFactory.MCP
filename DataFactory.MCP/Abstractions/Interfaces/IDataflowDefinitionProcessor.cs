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
    /// Adds a connection to an existing dataflow definition
    /// </summary>
    /// <param name="definition">The dataflow definition to modify</param>
    /// <param name="connection">The connection to add</param>
    /// <param name="connectionId">The connection ID</param>
    /// <returns>Updated dataflow definition</returns>
    DataflowDefinition AddConnectionToDefinition(
        DataflowDefinition definition,
        Connection connection,
        string connectionId,
        string? clusterId);
}