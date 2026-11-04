using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models;
using DataFactory.MCP.Models.Dataflow.BackgroundTask;

namespace DataFactory.MCP.Handlers.Dataflow;

public class DataflowRefreshHandler(IDataflowRefreshService refreshService)
{
    public async Task<ToolResult<DataflowRefreshResult>> StartAsync(
        string workspaceId,
        string dataflowId,
        string? displayName = null,
        string executeOption = ExecuteOptions.SkipApplyChanges)
    {
        if (string.IsNullOrWhiteSpace(workspaceId))
            return ToolResult<DataflowRefreshResult>.Failure(Messages.InvalidParameterEmpty("workspaceId"), "validation");
        if (string.IsNullOrWhiteSpace(dataflowId))
            return ToolResult<DataflowRefreshResult>.Failure(Messages.InvalidParameterEmpty("dataflowId"), "validation");

        try
        {
            var result = await refreshService.StartRefreshAsync(
                workspaceId,
                dataflowId,
                displayName,
                executeOption);
            return ToolResult<DataflowRefreshResult>.Success(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ToolResult<DataflowRefreshResult>.Failure(
                string.Format(Messages.AuthenticationErrorTemplate, ex.Message), "auth");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return ToolResult<DataflowRefreshResult>.Failure(
                "Authentication failed. Please check your credentials.", "auth");
        }
        catch (HttpRequestException ex)
        {
            return ToolResult<DataflowRefreshResult>.Failure(
                $"Failed to start dataflow refresh: {ex.Message}", "http");
        }
        catch (Exception ex)
        {
            return ToolResult<DataflowRefreshResult>.Failure(
                $"Unexpected error starting dataflow refresh: {ex.Message}", "operation");
        }
    }
}
