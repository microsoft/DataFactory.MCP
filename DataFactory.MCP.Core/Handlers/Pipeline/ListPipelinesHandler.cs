using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Models.Pipeline;

namespace DataFactory.MCP.Handlers.Pipeline;

public record ListPipelinesResult(
    string WorkspaceId,
    int PipelineCount,
    string? ContinuationToken,
    string? ContinuationUri,
    bool HasMoreResults,
    IEnumerable<object> Pipelines,
    List<Models.Pipeline.Pipeline> RawPipelines);

public class ListPipelinesHandler
{
    private readonly IFabricPipelineService _pipelineService;
    private readonly IValidationService _validationService;

    public ListPipelinesHandler(
        IFabricPipelineService pipelineService,
        IValidationService validationService)
    {
        _pipelineService = pipelineService;
        _validationService = validationService;
    }

    public async Task<ToolResult<ListPipelinesResult>> ExecuteAsync(
        string workspaceId, string? continuationToken = null)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));

            var response = await _pipelineService.ListPipelinesAsync(workspaceId, continuationToken);

            var result = new ListPipelinesResult(
                WorkspaceId: workspaceId,
                PipelineCount: response.Value.Count,
                ContinuationToken: response.ContinuationToken,
                ContinuationUri: response.ContinuationUri,
                HasMoreResults: !string.IsNullOrEmpty(response.ContinuationToken),
                Pipelines: response.Value.Select(p => p.ToFormattedInfo()).ToList(),
                RawPipelines: response.Value);

            return ToolResult<ListPipelinesResult>.Success(result);
        }
        catch (ArgumentException ex)
        {
            return ToolResult<ListPipelinesResult>.Failure(ex.Message, "validation");
        }
        catch (UnauthorizedAccessException ex)
        {
            return ToolResult<ListPipelinesResult>.Failure(ex.ToAuthenticationError().Message!, "auth");
        }
        catch (HttpRequestException ex)
        {
            return ToolResult<ListPipelinesResult>.Failure(ex.ToHttpError().Message!, "http");
        }
        catch (Exception ex)
        {
            return ToolResult<ListPipelinesResult>.Failure(ex.Message, "operation");
        }
    }
}
