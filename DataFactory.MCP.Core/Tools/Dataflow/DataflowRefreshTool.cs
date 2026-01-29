using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Models.Dataflow.BackgroundTask;

namespace DataFactory.MCP.Tools.Dataflow;

/// <summary>
/// MCP Tool for managing dataflow refresh operations.
/// Supports background refresh with automatic monitoring and notifications.
/// </summary>
[McpServerToolType]
public class DataflowRefreshTool
{
    private readonly IDataflowRefreshService _dataflowRefreshService;
    private readonly IValidationService _validationService;

    public DataflowRefreshTool(
        IDataflowRefreshService dataflowRefreshService,
        IValidationService validationService)
    {
        _dataflowRefreshService = dataflowRefreshService;
        _validationService = validationService;
    }

    [McpServerTool, Description(@"Start a dataflow refresh in the background. Returns immediately with task info.
You'll receive a notification when the refresh completes.
You can continue chatting while the refresh runs in the background.

Use this for long-running refresh operations. For quick status checks, use RefreshDataflowStatus.")]
    public async Task<string> RefreshDataflowBackground(
        McpServer mcpServer,
        [Description("The workspace ID containing the dataflow (required)")] string workspaceId,
        [Description("The dataflow ID to refresh (required)")] string dataflowId,
        [Description("User-friendly name for notifications (optional, defaults to dataflow ID)")] string? displayName = null,
        [Description("Execute option: 'SkipApplyChanges' (default, faster) or 'ApplyChangesIfNeeded' (applies pending changes first)")] string executeOption = "SkipApplyChanges")
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(dataflowId, nameof(dataflowId));

            // Pass the MCP server (session) to the service for notifications
            var result = await _dataflowRefreshService.StartRefreshAsync(
                mcpServer,
                workspaceId,
                dataflowId,
                displayName,
                executeOption);

            var response = new
            {
                Success = !result.IsComplete || result.IsSuccess,
                Message = result.IsComplete
                    ? $"Refresh {result.Status}: {result.ErrorMessage}"
                    : $"Refresh started in background. You'll be notified when complete.",
                Status = result.Status,
                TaskInfo = result.Context != null ? new
                {
                    WorkspaceId = result.Context.WorkspaceId,
                    DataflowId = result.Context.DataflowId,
                    JobInstanceId = result.Context.JobInstanceId,
                    DisplayName = result.Context.DisplayName,
                    StartedAt = result.Context.StartedAtUtc.ToString("o"),
                    EstimatedPollInterval = $"{result.Context.RetryAfterSeconds} seconds"
                } : null,
                Hint = result.IsComplete
                    ? null
                    : "Continue chatting - you'll receive a notification when the refresh completes. " +
                      "Use RefreshDataflowStatus to manually check progress if needed."
            };

            return response.ToMcpJson();
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
            return ex.ToOperationError("starting dataflow refresh").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Check the status of a dataflow refresh operation.
Use this to manually poll for status if you started a refresh with RefreshDataflowBackground.
Returns the current status including whether it's complete and any error information.")]
    public async Task<string> RefreshDataflowStatus(
        [Description("The workspace ID containing the dataflow (required)")] string workspaceId,
        [Description("The dataflow ID being refreshed (required)")] string dataflowId,
        [Description("The job instance ID from RefreshDataflowBackground result (required)")] string jobInstanceId)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(dataflowId, nameof(dataflowId));
            _validationService.ValidateRequiredString(jobInstanceId, nameof(jobInstanceId));

            var context = new DataflowRefreshContext
            {
                WorkspaceId = workspaceId,
                DataflowId = dataflowId,
                JobInstanceId = jobInstanceId
            };

            var result = await _dataflowRefreshService.GetStatusAsync(context);

            var response = new
            {
                IsComplete = result.IsComplete,
                IsSuccess = result.IsSuccess,
                Status = result.Status,
                WorkspaceId = workspaceId,
                DataflowId = dataflowId,
                JobInstanceId = jobInstanceId,
                EndTimeUtc = result.EndTimeUtc?.ToString("o"),
                Duration = result.DurationFormatted,
                FailureReason = result.FailureReason,
                Message = result.IsComplete
                    ? (result.IsSuccess
                        ? $"Refresh completed successfully in {result.DurationFormatted}"
                        : $"Refresh {result.Status}: {result.FailureReason}")
                    : $"Refresh still in progress (status: {result.Status})"
            };

            return response.ToMcpJson();
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
            return ex.ToOperationError("checking refresh status").ToMcpJson();
        }
    }
}
