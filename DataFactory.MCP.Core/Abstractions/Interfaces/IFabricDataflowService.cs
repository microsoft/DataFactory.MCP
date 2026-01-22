using DataFactory.MCP.Models.Dataflow;
using DataFactory.MCP.Models.Dataflow.Definition;
using DataFactory.MCP.Models.Dataflow.Query;
using DataFactory.MCP.Models.Connection;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Service for interacting with Microsoft Fabric Dataflows API
/// </summary>
public interface IFabricDataflowService
{
    /// <summary>
    /// Lists all dataflows from the specified workspace
    /// </summary>
    /// <param name="workspaceId">The workspace ID to list dataflows from</param>
    /// <param name="continuationToken">A token for retrieving the next page of results</param>
    /// <returns>List of dataflows from the workspace</returns>
    Task<ListDataflowsResponse> ListDataflowsAsync(
        string workspaceId,
        string? continuationToken = null);

    /// <summary>
    /// Creates a new dataflow in the specified workspace
    /// </summary>
    /// <param name="workspaceId">The workspace ID where the dataflow will be created</param>
    /// <param name="request">The create dataflow request</param>
    /// <returns>The created dataflow information</returns>
    Task<CreateDataflowResponse> CreateDataflowAsync(
        string workspaceId,
        CreateDataflowRequest request);

    /// <summary>
    /// Executes a query against a dataflow and returns the results
    /// </summary>
    /// <param name="workspaceId">The workspace ID containing the dataflow</param>
    /// <param name="dataflowId">The dataflow ID to execute the query against</param>
    /// <param name="request">The execute query request containing the M query</param>
    /// <returns>The query execution results in Apache Arrow format</returns>
    Task<ExecuteDataflowQueryResponse> ExecuteQueryAsync(
        string workspaceId,
        string dataflowId,
        ExecuteDataflowQueryRequest request);

    /// <summary>
    /// Gets the raw definition of a dataflow from the API
    /// </summary>
    /// <param name="workspaceId">The workspace ID containing the dataflow</param>
    /// <param name="dataflowId">The dataflow ID to get the definition for</param>
    /// <returns>The raw dataflow definition from API</returns>
    Task<DataflowDefinition> GetDataflowDefinitionAsync(
        string workspaceId,
        string dataflowId);

    /// <summary>
    /// Gets the decoded definition of a dataflow with human-readable content
    /// </summary>
    /// <param name="workspaceId">The workspace ID containing the dataflow</param>
    /// <param name="dataflowId">The dataflow ID to get the definition for</param>
    /// <returns>The decoded dataflow definition with readable content</returns>
    Task<DecodedDataflowDefinition> GetDecodedDataflowDefinitionAsync(
        string workspaceId,
        string dataflowId);

    /// <summary>
    /// Updates a dataflow definition via the API
    /// </summary>
    /// <param name="workspaceId">The workspace ID containing the dataflow</param>
    /// <param name="dataflowId">The dataflow ID to update</param>
    /// <param name="definition">The updated dataflow definition</param>
    /// <exception cref="HttpRequestException">Thrown when the API request fails</exception>
    Task UpdateDataflowDefinitionAsync(
        string workspaceId,
        string dataflowId,
        DataflowDefinition definition);

    /// <summary>
    /// Adds one or more connections to an existing dataflow by updating its definition
    /// </summary>
    /// <param name="workspaceId">The workspace ID containing the dataflow</param>
    /// <param name="dataflowId">The dataflow ID to update</param>
    /// <param name="connections">List of tuples containing (connectionId, connection) for each connection to add</param>
    /// <returns>Update operation result</returns>
    Task<UpdateDataflowDefinitionResponse> AddConnectionsToDataflowAsync(
        string workspaceId,
        string dataflowId,
        IEnumerable<(string ConnectionId, DataFactory.MCP.Models.Connection.Connection Connection)> connections);

    /// <summary>
    /// Adds or updates a query in an existing dataflow by updating its definition
    /// </summary>
    /// <param name="workspaceId">The workspace ID containing the dataflow</param>
    /// <param name="dataflowId">The dataflow ID to update</param>
    /// <param name="queryName">The name of the query to add or update</param>
    /// <param name="mCode">The M (Power Query) code for the query</param>
    /// <param name="attribute">Optional attribute for the query (e.g., [DataDestinations = ...])</param>
    /// <param name="sectionAttribute">Optional section-level attribute (e.g., [StagingDefinition = [Kind = "FastCopy"]])</param>
    /// <returns>Update operation result</returns>
    Task<UpdateDataflowDefinitionResponse> AddOrUpdateQueryAsync(
        string workspaceId,
        string dataflowId,
        string queryName,
        string mCode,
        string? attribute = null,
        string? sectionAttribute = null);

    /// <summary>
    /// Adds or updates multiple queries in an existing dataflow in a single operation.
    /// More efficient than calling AddOrUpdateQueryAsync multiple times as it fetches
    /// the definition once, applies all changes in memory, and saves once.
    /// </summary>
    /// <param name="workspaceId">The workspace ID containing the dataflow</param>
    /// <param name="dataflowId">The dataflow ID to update</param>
    /// <param name="queries">List of queries to add/update as tuples of (queryName, mCode, attribute)</param>
    /// <param name="sectionAttribute">Optional section-level attribute (e.g., [StagingDefinition = [Kind = "FastCopy"]])</param>
    /// <returns>Update operation result</returns>
    Task<UpdateDataflowDefinitionResponse> AddOrUpdateQueriesBatchAsync(
        string workspaceId,
        string dataflowId,
        IList<(string QueryName, string MCode, string? Attribute)> queries,
        string? sectionAttribute = null);

    /// <summary>
    /// Syncs a dataflow with a new M document by replacing the entire mashup content.
    /// This is a declarative approach: the provided document becomes the complete state.
    /// Queries not in the new document will be removed from metadata.
    /// </summary>
    /// <param name="workspaceId">The workspace ID containing the dataflow</param>
    /// <param name="dataflowId">The dataflow ID to sync</param>
    /// <param name="newMashupDocument">The complete M section document</param>
    /// <param name="parsedQueries">List of parsed queries from the document</param>
    /// <returns>Update operation result</returns>
    Task<UpdateDataflowDefinitionResponse> SyncMashupDocumentAsync(
        string workspaceId,
        string dataflowId,
        string newMashupDocument,
        IList<(string QueryName, string MCode, string? Attribute)> parsedQueries);
}