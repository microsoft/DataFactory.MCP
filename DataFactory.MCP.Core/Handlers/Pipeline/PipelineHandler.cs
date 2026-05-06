using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models;
using DataFactory.MCP.Models.Pipeline;

namespace DataFactory.MCP.Handlers.Pipeline;

public record ListPipelinesResult(
    string WorkspaceId,
    int PipelineCount,
    string? ContinuationToken,
    string? ContinuationUri,
    bool HasMoreResults,
    IReadOnlyList<Models.Pipeline.Pipeline> Pipelines);

public record CreatePipelineResult(CreatePipelineResponse Pipeline);

public record GetPipelineResult(Models.Pipeline.Pipeline Pipeline);

public record RunPipelineResult(string? LocationUrl, string? JobInstanceId);

public class PipelineHandler(IFabricPipelineService pipelineService)
{
    public async Task<ToolResult<ListPipelinesResult>> ListAsync(string workspaceId, string? continuationToken = null)
    {
        if (string.IsNullOrWhiteSpace(workspaceId))
            return ToolResult<ListPipelinesResult>.Failure(Messages.InvalidParameterEmpty("workspaceId"), "validation");

        try
        {
            var response = await pipelineService.ListPipelinesAsync(workspaceId, continuationToken);
            var result = new ListPipelinesResult(
                WorkspaceId: workspaceId,
                PipelineCount: response.Value.Count,
                ContinuationToken: response.ContinuationToken,
                ContinuationUri: response.ContinuationUri,
                HasMoreResults: !string.IsNullOrEmpty(response.ContinuationToken),
                Pipelines: response.Value);
            return ToolResult<ListPipelinesResult>.Success(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ToolResult<ListPipelinesResult>.Failure(string.Format(Messages.AuthenticationErrorTemplate, ex.Message), "auth");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return ToolResult<ListPipelinesResult>.Failure(string.Format(Messages.AuthenticationErrorTemplate, ex.Message), "auth");
        }
        catch (HttpRequestException ex)
        {
            return ToolResult<ListPipelinesResult>.Failure($"Failed to list pipelines: {ex.Message}", "http");
        }
        catch (Exception ex)
        {
            return ToolResult<ListPipelinesResult>.Failure($"Unexpected error listing pipelines: {ex.Message}", "operation");
        }
    }

    public async Task<ToolResult<CreatePipelineResult>> CreateAsync(string workspaceId, string displayName, string? description = null, string? folderId = null)
    {
        if (string.IsNullOrWhiteSpace(workspaceId))
            return ToolResult<CreatePipelineResult>.Failure(Messages.InvalidParameterEmpty("workspaceId"), "validation");
        if (string.IsNullOrWhiteSpace(displayName))
            return ToolResult<CreatePipelineResult>.Failure(Messages.InvalidParameterEmpty("displayName"), "validation");

        try
        {
            var request = new CreatePipelineRequest
            {
                DisplayName = displayName,
                Description = description,
                FolderId = folderId
            };
            var pipeline = await pipelineService.CreatePipelineAsync(workspaceId, request);
            return ToolResult<CreatePipelineResult>.Success(new CreatePipelineResult(pipeline));
        }
        catch (UnauthorizedAccessException ex)
        {
            return ToolResult<CreatePipelineResult>.Failure(string.Format(Messages.AuthenticationErrorTemplate, ex.Message), "auth");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return ToolResult<CreatePipelineResult>.Failure(string.Format(Messages.AuthenticationErrorTemplate, ex.Message), "auth");
        }
        catch (HttpRequestException ex)
        {
            return ToolResult<CreatePipelineResult>.Failure($"Failed to create pipeline: {ex.Message}", "http");
        }
        catch (Exception ex)
        {
            return ToolResult<CreatePipelineResult>.Failure($"Unexpected error creating pipeline: {ex.Message}", "operation");
        }
    }

    public async Task<ToolResult<GetPipelineResult>> GetAsync(string workspaceId, string pipelineId)
    {
        if (string.IsNullOrWhiteSpace(workspaceId))
            return ToolResult<GetPipelineResult>.Failure(Messages.InvalidParameterEmpty("workspaceId"), "validation");
        if (string.IsNullOrWhiteSpace(pipelineId))
            return ToolResult<GetPipelineResult>.Failure(Messages.InvalidParameterEmpty("pipelineId"), "validation");

        try
        {
            var pipeline = await pipelineService.GetPipelineAsync(workspaceId, pipelineId);
            return ToolResult<GetPipelineResult>.Success(new GetPipelineResult(pipeline));
        }
        catch (UnauthorizedAccessException ex)
        {
            return ToolResult<GetPipelineResult>.Failure(string.Format(Messages.AuthenticationErrorTemplate, ex.Message), "auth");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return ToolResult<GetPipelineResult>.Failure(string.Format(Messages.AuthenticationErrorTemplate, ex.Message), "auth");
        }
        catch (HttpRequestException ex)
        {
            return ToolResult<GetPipelineResult>.Failure($"Failed to get pipeline: {ex.Message}", "http");
        }
        catch (Exception ex)
        {
            return ToolResult<GetPipelineResult>.Failure($"Unexpected error getting pipeline: {ex.Message}", "operation");
        }
    }

    public async Task<ToolResult<RunPipelineResult>> RunAsync(string workspaceId, string pipelineId, object? executionData = null)
    {
        if (string.IsNullOrWhiteSpace(workspaceId))
            return ToolResult<RunPipelineResult>.Failure(Messages.InvalidParameterEmpty("workspaceId"), "validation");
        if (string.IsNullOrWhiteSpace(pipelineId))
            return ToolResult<RunPipelineResult>.Failure(Messages.InvalidParameterEmpty("pipelineId"), "validation");

        try
        {
            var location = await pipelineService.RunPipelineAsync(workspaceId, pipelineId, executionData);

            // Extract job instance ID from the Location header URL
            string? jobInstanceId = null;
            if (!string.IsNullOrEmpty(location))
            {
                var segments = new Uri(location).Segments;
                jobInstanceId = segments.LastOrDefault()?.TrimEnd('/');
            }

            return ToolResult<RunPipelineResult>.Success(new RunPipelineResult(location, jobInstanceId));
        }
        catch (UnauthorizedAccessException ex)
        {
            return ToolResult<RunPipelineResult>.Failure(string.Format(Messages.AuthenticationErrorTemplate, ex.Message), "auth");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return ToolResult<RunPipelineResult>.Failure(string.Format(Messages.AuthenticationErrorTemplate, ex.Message), "auth");
        }
        catch (HttpRequestException ex)
        {
            return ToolResult<RunPipelineResult>.Failure($"Failed to run pipeline: {ex.Message}", "http");
        }
        catch (Exception ex)
        {
            return ToolResult<RunPipelineResult>.Failure($"Unexpected error running pipeline: {ex.Message}", "operation");
        }
    }
}
