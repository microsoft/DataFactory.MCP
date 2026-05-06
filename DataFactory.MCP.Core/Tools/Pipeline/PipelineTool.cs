using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Handlers;
using DataFactory.MCP.Handlers.Pipeline;
using DataFactory.MCP.Models.Pipeline;
using DataFactory.MCP.Models.Pipeline.Definition;
using DataFactory.MCP.Models.Pipeline.Schedule;

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
    private readonly PipelineHandler _pipelineHandler;

    public PipelineTool(
        IFabricPipelineService pipelineService,
        IValidationService validationService,
        PipelineHandler pipelineHandler)
    {
        _pipelineService = pipelineService;
        _validationService = validationService;
        _pipelineHandler = pipelineHandler;
    }

    [McpServerTool, Description(@"Returns a list of Pipelines from the specified workspace. This API supports pagination.")]
    public async Task<string> ListPipelinesAsync(
        [Description("The workspace ID to list pipelines from (required)")] string workspaceId,
        [Description("A token for retrieving the next page of results (optional)")] string? continuationToken = null)
    {
        var result = await _pipelineHandler.ListAsync(workspaceId, continuationToken);
        if (result.IsSuccess)
        {
            var value = result.Value!;
            return new
            {
                value.WorkspaceId,
                value.PipelineCount,
                value.ContinuationToken,
                value.ContinuationUri,
                value.HasMoreResults,
                Pipelines = value.Pipelines.Select(p => p.ToFormattedInfo())
            }.ToMcpJson();
        }
        return result.ToErrorResponse("listing pipelines").ToMcpJson();
    }

    [McpServerTool, Description(@"Creates a Pipeline in the specified workspace.")]
    public async Task<string> CreatePipelineAsync(
        [Description("The workspace ID where the pipeline will be created (required)")] string workspaceId,
        [Description("The Pipeline display name (required)")] string displayName,
        [Description("The Pipeline description (optional, max 256 characters)")] string? description = null,
        [Description("The folder ID where the pipeline will be created (optional, defaults to workspace root)")] string? folderId = null)
    {
        var result = await _pipelineHandler.CreateAsync(workspaceId, displayName, description, folderId);
        if (result.IsSuccess)
        {
            var response = result.Value!.Pipeline;
            return new
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
            }.ToMcpJson();
        }
        return result.ToErrorResponse("creating pipeline").ToMcpJson();
    }

    [McpServerTool, Description(@"Gets the metadata of a Pipeline by ID.")]
    public async Task<string> GetPipelineAsync(
        [Description("The workspace ID containing the pipeline (required)")] string workspaceId,
        [Description("The pipeline ID to retrieve (required)")] string pipelineId)
    {
        var result = await _pipelineHandler.GetAsync(workspaceId, pipelineId);
        if (result.IsSuccess)
        {
            return result.Value!.Pipeline.ToFormattedInfo().ToMcpJson();
        }
        return result.ToErrorResponse("getting pipeline").ToMcpJson();
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

    [McpServerTool, Description(@"Runs a Pipeline on demand. Returns a job instance ID that can be used to track the run status with GetPipelineRunStatusAsync.")]
    public async Task<string> RunPipelineAsync(
        [Description("The workspace ID containing the pipeline (required)")] string workspaceId,
        [Description("The pipeline ID to run (required)")] string pipelineId,
        [Description("Optional execution data as JSON string for parameterized pipeline runs (optional)")] string? executionDataJson = null)
    {
        object? executionData = null;
        if (!string.IsNullOrEmpty(executionDataJson))
        {
            try
            {
                executionData = JsonSerializer.Deserialize<object>(executionDataJson);
            }
            catch (JsonException ex)
            {
                return ToolResult<object>.Failure($"Invalid executionData JSON format: {ex.Message}", "validation")
                    .ToErrorResponse("running pipeline").ToMcpJson();
            }
        }

        var result = await _pipelineHandler.RunAsync(workspaceId, pipelineId, executionData);
        if (result.IsSuccess)
        {
            var value = result.Value!;
            return new
            {
                Success = true,
                Message = "Pipeline run triggered successfully",
                PipelineId = pipelineId,
                WorkspaceId = workspaceId,
                JobInstanceId = value.JobInstanceId,
                LocationUrl = value.LocationUrl,
                Hint = "Use GetPipelineRunStatusAsync with the jobInstanceId to check the run status"
            }.ToMcpJson();
        }
        return result.ToErrorResponse("running pipeline").ToMcpJson();
    }

    [McpServerTool, Description(@"Gets the status of a Pipeline run (job instance). Use the jobInstanceId returned from RunPipelineAsync to check the run status. Possible statuses: NotStarted, InProgress, Completed, Failed, Cancelled, Deduped.")]
    public async Task<string> GetPipelineRunStatusAsync(
        [Description("The workspace ID containing the pipeline (required)")] string workspaceId,
        [Description("The pipeline ID (required)")] string pipelineId,
        [Description("The job instance ID returned from RunPipelineAsync (required)")] string jobInstanceId)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(pipelineId, nameof(pipelineId));
            _validationService.ValidateRequiredString(jobInstanceId, nameof(jobInstanceId));

            var jobInstance = await _pipelineService.GetPipelineJobInstanceAsync(workspaceId, pipelineId, jobInstanceId);

            var result = new
            {
                Success = true,
                JobInstanceId = jobInstance.Id,
                PipelineId = pipelineId,
                WorkspaceId = workspaceId,
                JobType = jobInstance.JobType,
                InvokeType = jobInstance.InvokeType,
                Status = jobInstance.Status,
                StartTimeUtc = jobInstance.StartTimeUtc,
                EndTimeUtc = jobInstance.EndTimeUtc,
                FailureReason = jobInstance.FailureReason
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
            return ex.ToOperationError("getting pipeline run status").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Creates a schedule for a Pipeline. Supports Cron (interval-based), Daily, Weekly, and Monthly schedule types. An item can have up to 20 schedules.")]
    public async Task<string> CreatePipelineScheduleAsync(
        [Description("The workspace ID containing the pipeline (required)")] string workspaceId,
        [Description("The pipeline ID to schedule (required)")] string pipelineId,
        [Description("Whether the schedule is enabled (required)")] bool enabled,
        [Description(@"The schedule configuration as JSON string (required). Supported types:
- Cron: {""type"":""Cron"",""startDateTime"":""2024-04-28T00:00:00"",""endDateTime"":""2024-04-30T23:59:00"",""localTimeZoneId"":""Central Standard Time"",""interval"":10}
- Daily: {""type"":""Daily"",""startDateTime"":""..."",""endDateTime"":""..."",""localTimeZoneId"":""..."",""times"":[""08:00"",""16:00""]}
- Weekly: {""type"":""Weekly"",""startDateTime"":""..."",""endDateTime"":""..."",""localTimeZoneId"":""..."",""weekdays"":[""Monday"",""Wednesday""],""times"":[""09:00""]}
- Monthly: {""type"":""Monthly"",""startDateTime"":""..."",""endDateTime"":""..."",""localTimeZoneId"":""..."",""occurrence"":{""occurrenceType"":""DayOfMonth"",""dayOfMonth"":15},""recurrence"":1,""times"":[""10:00""]}")] string configurationJson)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(pipelineId, nameof(pipelineId));
            _validationService.ValidateRequiredString(configurationJson, nameof(configurationJson));

            object configuration;
            try
            {
                configuration = JsonSerializer.Deserialize<object>(configurationJson)
                    ?? throw new ArgumentException("Configuration JSON cannot be null");
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid configuration JSON format: {ex.Message}");
            }

            var request = new CreateScheduleRequest
            {
                Enabled = enabled,
                Configuration = configuration
            };

            var schedule = await _pipelineService.CreatePipelineScheduleAsync(workspaceId, pipelineId, request);

            var result = new
            {
                Success = true,
                Message = "Pipeline schedule created successfully",
                ScheduleId = schedule.Id,
                PipelineId = pipelineId,
                WorkspaceId = workspaceId,
                Enabled = schedule.Enabled,
                CreatedDateTime = schedule.CreatedDateTime,
                Configuration = schedule.Configuration,
                Owner = schedule.Owner
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
            return ex.ToOperationError("creating pipeline schedule").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Lists all schedules for a Pipeline. Returns the schedule configurations, status, and owner information.")]
    public async Task<string> ListPipelineSchedulesAsync(
        [Description("The workspace ID containing the pipeline (required)")] string workspaceId,
        [Description("The pipeline ID to list schedules for (required)")] string pipelineId,
        [Description("A token for retrieving the next page of results (optional)")] string? continuationToken = null)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(pipelineId, nameof(pipelineId));

            var response = await _pipelineService.ListPipelineSchedulesAsync(workspaceId, pipelineId, continuationToken);

            if (!response.Value.Any())
            {
                return $"No schedules found for pipeline '{pipelineId}' in workspace '{workspaceId}'.";
            }

            var result = new
            {
                PipelineId = pipelineId,
                WorkspaceId = workspaceId,
                ScheduleCount = response.Value.Count,
                ContinuationToken = response.ContinuationToken,
                ContinuationUri = response.ContinuationUri,
                HasMoreResults = !string.IsNullOrEmpty(response.ContinuationToken),
                Schedules = response.Value.Select(s => new
                {
                    Id = s.Id,
                    Enabled = s.Enabled,
                    CreatedDateTime = s.CreatedDateTime,
                    Configuration = s.Configuration,
                    Owner = s.Owner
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
            return ex.ToOperationError("listing pipeline schedules").ToMcpJson();
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
