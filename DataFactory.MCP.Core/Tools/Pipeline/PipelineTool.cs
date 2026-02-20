using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Models.Pipeline;
using DataFactory.MCP.Models.Pipeline.Definition;

namespace DataFactory.MCP.Tools.Pipeline;

/// <summary>
/// MCP Tool for managing Microsoft Fabric Pipelines.
/// Handles CRUD operations and definition management.
/// </summary>
[McpServerToolType]
public class PipelineTool
{
    private readonly IFabricPipelineService _pipelineService;
    private readonly IValidationService _validationService;

    public PipelineTool(
        IFabricPipelineService pipelineService,
        IValidationService validationService)
    {
        _pipelineService = pipelineService;
        _validationService = validationService;
    }

    [McpServerTool, Description(@"Returns a list of Pipelines from the specified workspace. This API supports pagination.")]
    public async Task<string> ListPipelinesAsync(
        [Description("The workspace ID to list pipelines from (required)")] string workspaceId,
        [Description("A token for retrieving the next page of results (optional)")] string? continuationToken = null)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));

            var response = await _pipelineService.ListPipelinesAsync(workspaceId, continuationToken);

            if (!response.Value.Any())
            {
                return $"No pipelines found in workspace '{workspaceId}'.";
            }

            var result = new
            {
                WorkspaceId = workspaceId,
                PipelineCount = response.Value.Count,
                ContinuationToken = response.ContinuationToken,
                ContinuationUri = response.ContinuationUri,
                HasMoreResults = !string.IsNullOrEmpty(response.ContinuationToken),
                Pipelines = response.Value.Select(p => p.ToFormattedInfo())
            };

            return result.ToMcpJson();
        }
        catch (ArgumentException ex)
        {
            return ex.ToValidationError().ToMcpJson();
        }
        catch (UnauthorizedAccessException ex)
        {
            return ex.ToAuthenticationError().ToMcpJson();
        }
        catch (HttpRequestException ex)
        {
            return ex.ToHttpError().ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("listing pipelines").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Creates a Pipeline in the specified workspace.")]
    public async Task<string> CreatePipelineAsync(
        [Description("The workspace ID where the pipeline will be created (required)")] string workspaceId,
        [Description("The Pipeline display name (required)")] string displayName,
        [Description("The Pipeline description (optional, max 256 characters)")] string? description = null,
        [Description("The folder ID where the pipeline will be created (optional, defaults to workspace root)")] string? folderId = null)
    {
        try
        {
            var request = new CreatePipelineRequest
            {
                DisplayName = displayName,
                Description = description,
                FolderId = folderId
            };

            var response = await _pipelineService.CreatePipelineAsync(workspaceId, request);

            var result = new
            {
                Success = true,
                Message = $"Pipeline '{displayName}' created successfully",
                PipelineId = response.Id,
                DisplayName = response.DisplayName,
                Description = response.Description,
                Type = response.Type,
                WorkspaceId = response.WorkspaceId,
                FolderId = response.FolderId,
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            return result.ToMcpJson();
        }
        catch (ArgumentException ex)
        {
            return ex.ToValidationError().ToMcpJson();
        }
        catch (UnauthorizedAccessException ex)
        {
            return ex.ToAuthenticationError().ToMcpJson();
        }
        catch (HttpRequestException ex)
        {
            return ex.ToHttpError().ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("creating pipeline").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Gets the metadata of a Pipeline by ID.")]
    public async Task<string> GetPipelineAsync(
        [Description("The workspace ID containing the pipeline (required)")] string workspaceId,
        [Description("The pipeline ID to retrieve (required)")] string pipelineId)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(pipelineId, nameof(pipelineId));

            var pipeline = await _pipelineService.GetPipelineAsync(workspaceId, pipelineId);

            return pipeline.ToFormattedInfo().ToMcpJson();
        }
        catch (ArgumentException ex)
        {
            return ex.ToValidationError().ToMcpJson();
        }
        catch (UnauthorizedAccessException ex)
        {
            return ex.ToAuthenticationError().ToMcpJson();
        }
        catch (HttpRequestException ex)
        {
            return ex.ToHttpError().ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("getting pipeline").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Gets the definition of a Pipeline. The definition contains the pipeline JSON configuration with base64-encoded parts.")]
    public async Task<string> GetPipelineDefinitionAsync(
        [Description("The workspace ID containing the pipeline (required)")] string workspaceId,
        [Description("The pipeline ID to get the definition for (required)")] string pipelineId)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(pipelineId, nameof(pipelineId));

            var definition = await _pipelineService.GetPipelineDefinitionAsync(workspaceId, pipelineId);

            var result = new
            {
                Success = true,
                PipelineId = pipelineId,
                WorkspaceId = workspaceId,
                PartsCount = definition.Parts.Count,
                Parts = definition.Parts.Select(p => new
                {
                    Path = p.Path,
                    PayloadType = p.PayloadType,
                    DecodedPayload = TryDecodeBase64(p.Payload)
                })
            };

            return result.ToMcpJson();
        }
        catch (ArgumentException ex)
        {
            return ex.ToValidationError().ToMcpJson();
        }
        catch (UnauthorizedAccessException ex)
        {
            return ex.ToAuthenticationError().ToMcpJson();
        }
        catch (HttpRequestException ex)
        {
            return ex.ToHttpError().ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("getting pipeline definition").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Updates the metadata (displayName and/or description) of a Pipeline.")]
    public async Task<string> UpdatePipelineAsync(
        [Description("The workspace ID containing the pipeline (required)")] string workspaceId,
        [Description("The pipeline ID to update (required)")] string pipelineId,
        [Description("The new display name (optional)")] string? displayName = null,
        [Description("The new description (optional)")] string? description = null)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(pipelineId, nameof(pipelineId));

            if (string.IsNullOrEmpty(displayName) && description == null)
            {
                throw new ArgumentException("At least one of displayName or description must be provided");
            }

            var request = new UpdatePipelineRequest
            {
                DisplayName = displayName,
                Description = description
            };

            var pipeline = await _pipelineService.UpdatePipelineAsync(workspaceId, pipelineId, request);

            var result = new
            {
                Success = true,
                Message = $"Pipeline '{pipeline.DisplayName}' updated successfully",
                Pipeline = pipeline.ToFormattedInfo()
            };

            return result.ToMcpJson();
        }
        catch (ArgumentException ex)
        {
            return ex.ToValidationError().ToMcpJson();
        }
        catch (UnauthorizedAccessException ex)
        {
            return ex.ToAuthenticationError().ToMcpJson();
        }
        catch (HttpRequestException ex)
        {
            return ex.ToHttpError().ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("updating pipeline").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Updates the definition of a Pipeline with the provided JSON content. The JSON will be base64-encoded and sent to the API.")]
    public async Task<string> UpdatePipelineDefinitionAsync(
        [Description("The workspace ID containing the pipeline (required)")] string workspaceId,
        [Description("The pipeline ID to update (required)")] string pipelineId,
        [Description("The pipeline definition JSON content (required)")] string definitionJson)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(pipelineId, nameof(pipelineId));
            _validationService.ValidateRequiredString(definitionJson, nameof(definitionJson));

            // Validate JSON format
            try
            {
                JsonDocument.Parse(definitionJson);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid JSON format: {ex.Message}");
            }

            // Encode the JSON as base64
            var base64Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(definitionJson));

            var definition = new PipelineDefinition
            {
                Parts = new List<PipelineDefinitionPart>
                {
                    new PipelineDefinitionPart
                    {
                        Path = "pipeline-content.json",
                        Payload = base64Payload,
                        PayloadType = "InlineBase64"
                    }
                }
            };

            await _pipelineService.UpdatePipelineDefinitionAsync(workspaceId, pipelineId, definition);

            var result = new
            {
                Success = true,
                PipelineId = pipelineId,
                WorkspaceId = workspaceId,
                Message = $"Pipeline definition updated successfully"
            };

            return result.ToMcpJson();
        }
        catch (ArgumentException ex)
        {
            return ex.ToValidationError().ToMcpJson();
        }
        catch (UnauthorizedAccessException ex)
        {
            return ex.ToAuthenticationError().ToMcpJson();
        }
        catch (HttpRequestException ex)
        {
            return ex.ToHttpError().ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("updating pipeline definition").ToMcpJson();
        }
    }

    /// <summary>
    /// Attempts to decode a base64-encoded string. Returns the decoded string or the original payload if decoding fails.
    /// </summary>
    private static string? TryDecodeBase64(string? payload)
    {
        if (string.IsNullOrEmpty(payload))
            return null;

        try
        {
            var bytes = Convert.FromBase64String(payload);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return payload;
        }
    }
}
