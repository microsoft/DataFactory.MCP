using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Models.CopyJob;
using DataFactory.MCP.Models.CopyJob.Definition;
using DataFactory.MCP.Models.Pipeline.Schedule;

namespace DataFactory.MCP.Tools.CopyJob;

/// <summary>
/// MCP Tool for managing Microsoft Fabric Copy Jobs.
/// Handles CRUD operations and definition management.
/// </summary>
[McpServerToolType]
public class CopyJobTool
{
    private readonly IFabricCopyJobService _copyJobService;
    private readonly IValidationService _validationService;

    public CopyJobTool(
        IFabricCopyJobService copyJobService,
        IValidationService validationService)
    {
        _copyJobService = copyJobService;
        _validationService = validationService;
    }

    [McpServerTool, Description(@"Returns a list of Copy Jobs from the specified workspace. This API supports pagination.")]
    public async Task<string> ListCopyJobsAsync(
        [Description("The workspace ID to list copy jobs from (required)")] string workspaceId,
        [Description("A token for retrieving the next page of results (optional)")] string? continuationToken = null)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));

            var response = await _copyJobService.ListCopyJobsAsync(workspaceId, continuationToken);

            if (!response.Value.Any())
            {
                return $"No copy jobs found in workspace '{workspaceId}'.";
            }

            var result = new
            {
                WorkspaceId = workspaceId,
                CopyJobCount = response.Value.Count,
                ContinuationToken = response.ContinuationToken,
                ContinuationUri = response.ContinuationUri,
                HasMoreResults = !string.IsNullOrEmpty(response.ContinuationToken),
                CopyJobs = response.Value.Select(c => c.ToFormattedInfo())
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
            return ex.ToOperationError("listing copy jobs").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Creates a Copy Job in the specified workspace.")]
    public async Task<string> CreateCopyJobAsync(
        [Description("The workspace ID where the copy job will be created (required)")] string workspaceId,
        [Description("The Copy Job display name (required)")] string displayName,
        [Description("The Copy Job description (optional, max 256 characters)")] string? description = null,
        [Description("The folder ID where the copy job will be created (optional, defaults to workspace root)")] string? folderId = null)
    {
        try
        {
            var request = new CreateCopyJobRequest
            {
                DisplayName = displayName,
                Description = description,
                FolderId = folderId
            };

            var response = await _copyJobService.CreateCopyJobAsync(workspaceId, request);

            var result = new
            {
                Success = true,
                Message = $"Copy job '{displayName}' created successfully",
                CopyJobId = response.Id,
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
            return ex.ToOperationError("creating copy job").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Gets the metadata of a Copy Job by ID.")]
    public async Task<string> GetCopyJobAsync(
        [Description("The workspace ID containing the copy job (required)")] string workspaceId,
        [Description("The copy job ID to retrieve (required)")] string copyJobId)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(copyJobId, nameof(copyJobId));

            var copyJob = await _copyJobService.GetCopyJobAsync(workspaceId, copyJobId);

            return copyJob.ToFormattedInfo().ToMcpJson();
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
            return ex.ToOperationError("getting copy job").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Gets the definition of a Copy Job. The definition contains the copy job JSON configuration with base64-encoded parts.")]
    public async Task<string> GetCopyJobDefinitionAsync(
        [Description("The workspace ID containing the copy job (required)")] string workspaceId,
        [Description("The copy job ID to get the definition for (required)")] string copyJobId)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(copyJobId, nameof(copyJobId));

            var definition = await _copyJobService.GetCopyJobDefinitionAsync(workspaceId, copyJobId);

            var result = new
            {
                Success = true,
                CopyJobId = copyJobId,
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
            return ex.ToOperationError("getting copy job definition").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Updates the metadata (displayName and/or description) of a Copy Job.")]
    public async Task<string> UpdateCopyJobAsync(
        [Description("The workspace ID containing the copy job (required)")] string workspaceId,
        [Description("The copy job ID to update (required)")] string copyJobId,
        [Description("The new display name (optional)")] string? displayName = null,
        [Description("The new description (optional)")] string? description = null)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(copyJobId, nameof(copyJobId));

            if (string.IsNullOrEmpty(displayName) && description == null)
            {
                throw new ArgumentException("At least one of displayName or description must be provided");
            }

            var request = new UpdateCopyJobRequest
            {
                DisplayName = displayName,
                Description = description
            };

            var copyJob = await _copyJobService.UpdateCopyJobAsync(workspaceId, copyJobId, request);

            var result = new
            {
                Success = true,
                Message = $"Copy job '{copyJob.DisplayName}' updated successfully",
                CopyJob = copyJob.ToFormattedInfo()
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
            return ex.ToOperationError("updating copy job").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Updates the definition of a Copy Job with the provided JSON content. The JSON will be base64-encoded and sent to the API.")]
    public async Task<string> UpdateCopyJobDefinitionAsync(
        [Description("The workspace ID containing the copy job (required)")] string workspaceId,
        [Description("The copy job ID to update (required)")] string copyJobId,
        [Description("The copy job definition JSON content (required)")] string definitionJson)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(copyJobId, nameof(copyJobId));
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

            var definition = new CopyJobDefinition
            {
                Parts = new List<CopyJobDefinitionPart>
                {
                    new CopyJobDefinitionPart
                    {
                        Path = "copyjob-content.json",
                        Payload = base64Payload,
                        PayloadType = "InlineBase64"
                    }
                }
            };

            await _copyJobService.UpdateCopyJobDefinitionAsync(workspaceId, copyJobId, definition);

            var result = new
            {
                Success = true,
                CopyJobId = copyJobId,
                WorkspaceId = workspaceId,
                Message = $"Copy job definition updated successfully"
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
            return ex.ToOperationError("updating copy job definition").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Runs a Copy Job on demand. Returns a job instance ID that can be used to track the run status with GetCopyJobRunStatusAsync.")]
    public async Task<string> RunCopyJobAsync(
        [Description("The workspace ID containing the copy job (required)")] string workspaceId,
        [Description("The copy job ID to run (required)")] string copyJobId,
        [Description("Optional execution data as JSON string for parameterized copy job runs (optional)")] string? executionDataJson = null)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(copyJobId, nameof(copyJobId));

            object? executionData = null;
            if (!string.IsNullOrEmpty(executionDataJson))
            {
                try
                {
                    executionData = JsonSerializer.Deserialize<object>(executionDataJson);
                }
                catch (JsonException ex)
                {
                    throw new ArgumentException($"Invalid executionData JSON format: {ex.Message}");
                }
            }

            var location = await _copyJobService.RunCopyJobAsync(workspaceId, copyJobId, executionData);

            // Extract job instance ID from the Location header URL
            string? jobInstanceId = null;
            if (!string.IsNullOrEmpty(location))
            {
                var segments = new Uri(location).Segments;
                jobInstanceId = segments.LastOrDefault()?.TrimEnd('/');
            }

            var result = new
            {
                Success = true,
                Message = $"Copy job run triggered successfully",
                CopyJobId = copyJobId,
                WorkspaceId = workspaceId,
                JobInstanceId = jobInstanceId,
                LocationUrl = location,
                Hint = "Use GetCopyJobRunStatusAsync with the jobInstanceId to check the run status"
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
            return ex.ToOperationError("running copy job").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Gets the status of a Copy Job run (job instance). Use the jobInstanceId returned from RunCopyJobAsync to check the run status. Possible statuses: NotStarted, InProgress, Completed, Failed, Cancelled, Deduped.")]
    public async Task<string> GetCopyJobRunStatusAsync(
        [Description("The workspace ID containing the copy job (required)")] string workspaceId,
        [Description("The copy job ID (required)")] string copyJobId,
        [Description("The job instance ID returned from RunCopyJobAsync (required)")] string jobInstanceId)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(copyJobId, nameof(copyJobId));
            _validationService.ValidateRequiredString(jobInstanceId, nameof(jobInstanceId));

            var jobInstance = await _copyJobService.GetCopyJobInstanceAsync(workspaceId, copyJobId, jobInstanceId);

            var result = new
            {
                Success = true,
                JobInstanceId = jobInstance.Id,
                CopyJobId = copyJobId,
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
            return ex.ToOperationError("getting copy job run status").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Creates a schedule for a Copy Job. Supports Cron (interval-based), Daily, Weekly, and Monthly schedule types. An item can have up to 20 schedules.")]
    public async Task<string> CreateCopyJobScheduleAsync(
        [Description("The workspace ID containing the copy job (required)")] string workspaceId,
        [Description("The copy job ID to schedule (required)")] string copyJobId,
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
            _validationService.ValidateRequiredString(copyJobId, nameof(copyJobId));
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

            var schedule = await _copyJobService.CreateCopyJobScheduleAsync(workspaceId, copyJobId, request);

            var result = new
            {
                Success = true,
                Message = "Copy job schedule created successfully",
                ScheduleId = schedule.Id,
                CopyJobId = copyJobId,
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
            return ex.ToOperationError("creating copy job schedule").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Lists all schedules for a Copy Job. Returns the schedule configurations, status, and owner information.")]
    public async Task<string> ListCopyJobSchedulesAsync(
        [Description("The workspace ID containing the copy job (required)")] string workspaceId,
        [Description("The copy job ID to list schedules for (required)")] string copyJobId,
        [Description("A token for retrieving the next page of results (optional)")] string? continuationToken = null)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(copyJobId, nameof(copyJobId));

            var response = await _copyJobService.ListCopyJobSchedulesAsync(workspaceId, copyJobId, continuationToken);

            if (!response.Value.Any())
            {
                return $"No schedules found for copy job '{copyJobId}' in workspace '{workspaceId}'.";
            }

            var result = new
            {
                CopyJobId = copyJobId,
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
            return ex.ToOperationError("listing copy job schedules").ToMcpJson();
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
