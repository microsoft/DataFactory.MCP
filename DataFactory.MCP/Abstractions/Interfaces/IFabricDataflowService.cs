using DataFactory.MCP.Models.Dataflow;

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


}