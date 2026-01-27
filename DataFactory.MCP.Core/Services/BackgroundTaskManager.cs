using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Infrastructure.Http;
using DataFactory.MCP.Models.Dataflow.BackgroundTask;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;

namespace DataFactory.MCP.Services;

/// <summary>
/// Manages background tasks and sends MCP notifications on completion.
/// Polls Fabric API for job status and notifies client when done.
/// </summary>
public class BackgroundTaskManager : IBackgroundTaskManager
{
    private const string LoggerName = "BackgroundTasks";
    private static readonly TimeSpan MaxTaskDuration = TimeSpan.FromHours(4);

    private readonly IMcpNotificationService _notificationService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BackgroundTaskManager> _logger;
    private readonly ConcurrentDictionary<string, BackgroundTaskInfo> _tasks = new();

    public BackgroundTaskManager(
        IMcpNotificationService notificationService,
        IHttpClientFactory httpClientFactory,
        ILogger<BackgroundTaskManager> logger)
    {
        _notificationService = notificationService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DataflowRefreshResult> StartDataflowRefreshAsync(
        McpSession session,
        string workspaceId,
        string dataflowId,
        string? displayName = null,
        string executeOption = ExecuteOptions.SkipApplyChanges,
        List<ItemJobParameter>? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        var taskId = Guid.NewGuid().ToString();
        var friendlyName = displayName ?? $"Dataflow {dataflowId[..Math.Min(8, dataflowId.Length)]}...";

        _logger.LogInformation("Starting background refresh for dataflow {DataflowId} in workspace {WorkspaceId}, taskId={TaskId}",
            dataflowId, workspaceId, taskId);

        try
        {
            // Call Fabric API to start the refresh
            var (jobInstanceId, location, retryAfter) = await StartRefreshJobAsync(
                workspaceId, dataflowId, executeOption, parameters);

            var context = new DataflowRefreshContext
            {
                WorkspaceId = workspaceId,
                DataflowId = dataflowId,
                JobInstanceId = jobInstanceId,
                Location = location,
                RetryAfterSeconds = retryAfter ?? 60,
                StartedAtUtc = DateTime.UtcNow,
                DisplayName = friendlyName
            };

            var taskInfo = new BackgroundTaskInfo
            {
                TaskId = taskId,
                TaskType = "DataflowRefresh",
                DisplayName = friendlyName,
                Status = "InProgress",
                StartedAtUtc = DateTime.UtcNow,
                Context = context
            };

            _tasks[taskId] = taskInfo;

            // Fire and forget - monitor in background, capturing the session
            _ = MonitorRefreshAsync(session, taskId, context);

            return new DataflowRefreshResult
            {
                IsComplete = false,
                Status = "InProgress",
                Context = context
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start dataflow refresh for {DataflowId}", dataflowId);

            return new DataflowRefreshResult
            {
                IsComplete = true,
                Status = "Failed",
                ErrorMessage = ex.Message,
                Context = new DataflowRefreshContext
                {
                    WorkspaceId = workspaceId,
                    DataflowId = dataflowId,
                    JobInstanceId = string.Empty,
                    DisplayName = friendlyName
                }
            };
        }
    }

    private async Task<(string JobInstanceId, string? Location, int? RetryAfter)> StartRefreshJobAsync(
        string workspaceId,
        string dataflowId,
        string executeOption,
        List<ItemJobParameter>? parameters)
    {
        var httpClient = _httpClientFactory.CreateClient(Configuration.HttpClientNames.FabricApi);

        var endpoint = $"workspaces/{workspaceId}/dataflows/{dataflowId}/jobs/Execute/instances";
        var url = FabricUrlBuilder.ForFabricApi().WithLiteralPath(endpoint).Build();

        var request = new RunOnDemandExecuteRequest();
        if (!string.IsNullOrEmpty(executeOption) || parameters?.Count > 0)
        {
            request.ExecutionData = new DataflowExecutionPayload
            {
                ExecuteOption = executeOption,
                Parameters = parameters
            };
        }

        var jsonContent = System.Text.Json.JsonSerializer.Serialize(request,
            Configuration.JsonSerializerOptionsProvider.FabricApi);
        var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

        _logger.LogInformation("Calling Fabric API: POST {Url}", url);

        var response = await httpClient.PostAsync(url, content);

        if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to start refresh. Status: {response.StatusCode}, Response: {errorContent}");
        }

        // Extract job instance ID from Location header
        var location = response.Headers.Location?.ToString();
        var jobInstanceId = ExtractJobInstanceId(location);

        // Extract Retry-After header
        int? retryAfter = null;
        if (response.Headers.TryGetValues("Retry-After", out var retryValues))
        {
            if (int.TryParse(retryValues.FirstOrDefault(), out var parsed))
            {
                retryAfter = parsed;
            }
        }

        _logger.LogInformation("Refresh started. JobInstanceId={JobInstanceId}, Location={Location}, RetryAfter={RetryAfter}",
            jobInstanceId, location, retryAfter);

        return (jobInstanceId, location, retryAfter);
    }

    private static string ExtractJobInstanceId(string? location)
    {
        if (string.IsNullOrEmpty(location))
            throw new InvalidOperationException("No Location header in response");

        // Location format: .../jobs/instances/{jobInstanceId}
        var segments = location.Split('/');
        var instancesIndex = Array.IndexOf(segments, "instances");
        if (instancesIndex >= 0 && instancesIndex < segments.Length - 1)
        {
            return segments[instancesIndex + 1];
        }

        throw new InvalidOperationException($"Could not parse job instance ID from Location: {location}");
    }

    private async Task MonitorRefreshAsync(McpSession session, string taskId, DataflowRefreshContext context)
    {
        var pollInterval = TimeSpan.FromSeconds(context.RetryAfterSeconds);
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Starting background monitoring for task {TaskId}, polling every {Interval}s",
            taskId, context.RetryAfterSeconds);

        try
        {
            while (DateTime.UtcNow - startTime < MaxTaskDuration)
            {
                await Task.Delay(pollInterval);

                var status = await GetRefreshStatusAsync(context);

                if (status.IsComplete)
                {
                    // Update task info
                    if (_tasks.TryGetValue(taskId, out var task))
                    {
                        _tasks[taskId] = task with
                        {
                            Status = status.Status,
                            CompletedAtUtc = status.EndTimeUtc ?? DateTime.UtcNow,
                            FailureReason = status.FailureReason
                        };
                    }

                    // Send notification to client
                    await SendCompletionNotificationAsync(session, context, status);

                    _logger.LogInformation("Task {TaskId} completed with status {Status}", taskId, status.Status);
                    return;
                }

                _logger.LogDebug("Task {TaskId} still in progress, status={Status}", taskId, status.Status);
            }

            // Timeout
            _logger.LogWarning("Task {TaskId} timed out after {Duration}", taskId, MaxTaskDuration);

            await SendCompletionNotificationAsync(session, context, new DataflowRefreshResult
            {
                IsComplete = true,
                Status = "Timeout",
                ErrorMessage = $"Refresh did not complete within {MaxTaskDuration.TotalHours} hours",
                Context = context
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring task {TaskId}", taskId);

            await SendCompletionNotificationAsync(session, context, new DataflowRefreshResult
            {
                IsComplete = true,
                Status = "Error",
                ErrorMessage = $"Monitoring failed: {ex.Message}",
                Context = context
            });
        }
    }

    /// <inheritdoc />
    public async Task<DataflowRefreshResult> GetRefreshStatusAsync(DataflowRefreshContext context)
    {
        var httpClient = _httpClientFactory.CreateClient(Configuration.HttpClientNames.FabricApi);

        // Use the generic job instance endpoint
        var endpoint = $"workspaces/{context.WorkspaceId}/items/{context.DataflowId}/jobs/instances/{context.JobInstanceId}";
        var url = FabricUrlBuilder.ForFabricApi().WithLiteralPath(endpoint).Build();

        _logger.LogDebug("Polling job status: GET {Url}", url);

        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var jobInstance = await response.Content.ReadFromJsonAsync<ItemJobInstance>(
            Configuration.JsonSerializerOptionsProvider.FabricApi);

        if (jobInstance == null)
        {
            throw new InvalidOperationException("Failed to deserialize job instance response");
        }

        var isComplete = jobInstance.Status is "Completed" or "Failed" or "Cancelled" or "Deduped";

        return new DataflowRefreshResult
        {
            IsComplete = isComplete,
            Status = jobInstance.Status ?? "Unknown",
            Context = context,
            EndTimeUtc = jobInstance.EndTimeUtc,
            FailureReason = jobInstance.FailureReason?.Message
        };
    }

    private async Task SendCompletionNotificationAsync(McpSession session, DataflowRefreshContext context, DataflowRefreshResult result)
    {
        var level = result.IsSuccess ? "info" : "error";

        var notificationData = new
        {
            type = "dataflow_refresh_completed",
            taskType = "DataflowRefresh",
            displayName = context.DisplayName,
            workspaceId = context.WorkspaceId,
            dataflowId = context.DataflowId,
            jobInstanceId = context.JobInstanceId,
            status = result.Status,
            isSuccess = result.IsSuccess,
            duration = result.DurationFormatted,
            startedAt = context.StartedAtUtc.ToString("o"),
            completedAt = result.EndTimeUtc?.ToString("o"),
            failureReason = result.FailureReason,
            message = result.IsSuccess
                ? $"✅ Dataflow refresh '{context.DisplayName}' completed successfully in {result.DurationFormatted}"
                : $"❌ Dataflow refresh '{context.DisplayName}' {result.Status}: {result.FailureReason ?? "Unknown error"}"
        };

        await _notificationService.SendNotificationAsync(session, level, LoggerName, notificationData);
    }

    /// <inheritdoc />
    public IReadOnlyList<BackgroundTaskInfo> GetAllTasks()
        => _tasks.Values.ToList().AsReadOnly();

    /// <inheritdoc />
    public BackgroundTaskInfo? GetTask(string taskId)
        => _tasks.TryGetValue(taskId, out var task) ? task : null;
}
