using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models;
using DataFactory.MCP.Models.Dataflow;

namespace DataFactory.MCP.Handlers.Dataflow;

public record ListDataflowsResult(
    string WorkspaceId,
    int DataflowCount,
    string? ContinuationToken,
    string? ContinuationUri,
    bool HasMoreResults,
    IReadOnlyList<Models.Dataflow.Dataflow> Dataflows);

public record CreateDataflowResult(CreateDataflowResponse Dataflow);

public class DataflowHandler(IFabricDataflowService dataflowService)
{
    public async Task<ToolResult<ListDataflowsResult>> ListAsync(string workspaceId, string? continuationToken = null)
    {
        if (string.IsNullOrWhiteSpace(workspaceId))
            return ToolResult<ListDataflowsResult>.Failure(Messages.InvalidParameterEmpty("workspaceId"), "validation");

        try
        {
            var response = await dataflowService.ListDataflowsAsync(workspaceId, continuationToken);
            var result = new ListDataflowsResult(
                WorkspaceId: workspaceId,
                DataflowCount: response.Value.Count,
                ContinuationToken: response.ContinuationToken,
                ContinuationUri: response.ContinuationUri,
                HasMoreResults: !string.IsNullOrEmpty(response.ContinuationToken),
                Dataflows: response.Value);
            return ToolResult<ListDataflowsResult>.Success(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ToolResult<ListDataflowsResult>.Failure(string.Format(Messages.AuthenticationErrorTemplate, ex.Message), "auth");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return ToolResult<ListDataflowsResult>.Failure("Authentication failed. Please check your credentials.", "auth");
        }
        catch (HttpRequestException ex)
        {
            return ToolResult<ListDataflowsResult>.Failure($"Failed to list dataflows: {ex.Message}", "http");
        }
        catch (Exception ex)
        {
            return ToolResult<ListDataflowsResult>.Failure($"Unexpected error listing dataflows: {ex.Message}", "operation");
        }
    }

    public async Task<ToolResult<CreateDataflowResult>> CreateAsync(string workspaceId, string displayName, string? description = null, string? folderId = null)
    {
        if (string.IsNullOrWhiteSpace(workspaceId))
            return ToolResult<CreateDataflowResult>.Failure(Messages.InvalidParameterEmpty("workspaceId"), "validation");
        if (string.IsNullOrWhiteSpace(displayName))
            return ToolResult<CreateDataflowResult>.Failure(Messages.InvalidParameterEmpty("displayName"), "validation");

        try
        {
            var request = new CreateDataflowRequest
            {
                DisplayName = displayName,
                Description = description,
                FolderId = folderId
            };
            var dataflow = await dataflowService.CreateDataflowAsync(workspaceId, request);
            return ToolResult<CreateDataflowResult>.Success(new CreateDataflowResult(dataflow));
        }
        catch (UnauthorizedAccessException ex)
        {
            return ToolResult<CreateDataflowResult>.Failure(string.Format(Messages.AuthenticationErrorTemplate, ex.Message), "auth");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return ToolResult<CreateDataflowResult>.Failure("Authentication failed. Please check your credentials.", "auth");
        }
        catch (HttpRequestException ex)
        {
            return ToolResult<CreateDataflowResult>.Failure($"Failed to create dataflow: {ex.Message}", "http");
        }
        catch (Exception ex)
        {
            return ToolResult<CreateDataflowResult>.Failure($"Unexpected error creating dataflow: {ex.Message}", "operation");
        }
    }
}
