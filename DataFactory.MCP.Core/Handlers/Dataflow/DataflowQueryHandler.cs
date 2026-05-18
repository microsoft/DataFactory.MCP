using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Models;
using DataFactory.MCP.Models.Dataflow.Query;

namespace DataFactory.MCP.Handlers.Dataflow;

public record ExecuteQueryResult(
    bool Success,
    object? Data,
    QueryResultSummary? Summary);

public class DataflowQueryHandler(IFabricDataflowService dataflowService)
{
    public async Task<ToolResult<ExecuteQueryResult>> ExecuteQueryAsync(
        string workspaceId,
        string dataflowId,
        string queryName,
        string customMashupDocument)
    {
        if (string.IsNullOrWhiteSpace(workspaceId))
            return ToolResult<ExecuteQueryResult>.Failure(Messages.InvalidParameterEmpty("workspaceId"), "validation");
        if (string.IsNullOrWhiteSpace(dataflowId))
            return ToolResult<ExecuteQueryResult>.Failure(Messages.InvalidParameterEmpty("dataflowId"), "validation");
        if (string.IsNullOrWhiteSpace(queryName))
            return ToolResult<ExecuteQueryResult>.Failure(Messages.InvalidParameterEmpty("queryName"), "validation");
        if (string.IsNullOrWhiteSpace(customMashupDocument))
            return ToolResult<ExecuteQueryResult>.Failure(Messages.InvalidParameterEmpty("customMashupDocument"), "validation");

        try
        {
            // Auto-wrap the query if it's not already in section format
            var wrappedQuery = customMashupDocument.WrapForDataflowQuery(queryName);

            var request = new ExecuteDataflowQueryRequest
            {
                QueryName = queryName,
                CustomMashupDocument = wrappedQuery
            };

            var response = await dataflowService.ExecuteQueryAsync(workspaceId, dataflowId, request);

            if (!response.Success)
            {
                return ToolResult<ExecuteQueryResult>.Failure(
                    $"Query execution failed for '{queryName}' in dataflow {dataflowId}: {response.Error}",
                    "operation");
            }

            var data = response.CreateArrowDataReport();
            var result = new ExecuteQueryResult(
                Success: true,
                Data: data,
                Summary: response.Summary);

            return ToolResult<ExecuteQueryResult>.Success(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ToolResult<ExecuteQueryResult>.Failure(
                string.Format(Messages.AuthenticationErrorTemplate, ex.Message), "auth");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return ToolResult<ExecuteQueryResult>.Failure(
                "Authentication failed. Please check your credentials.", "auth");
        }
        catch (HttpRequestException ex)
        {
            return ToolResult<ExecuteQueryResult>.Failure(
                $"Failed to execute dataflow query: {ex.Message}", "http");
        }
        catch (Exception ex)
        {
            return ToolResult<ExecuteQueryResult>.Failure(
                $"Unexpected error executing dataflow query: {ex.Message}", "operation");
        }
    }
}
