using DataFactory.MCP.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;

namespace DataFactory.MCP.Services.BackgroundTasks;

/// <summary>
/// Executes background jobs and sends notifications on completion.
/// Single Responsibility: orchestration only - delegates to job for execution.
/// </summary>
public class BackgroundJobRunner : IBackgroundJobRunner
{
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MaxJobDuration = TimeSpan.FromHours(4);

    private readonly IMcpSessionAccessor _sessionAccessor;
    private readonly IBackgroundTaskTracker _taskTracker;
    private readonly IUserNotificationService _notificationService;
    private readonly ILogger<BackgroundJobRunner> _logger;

    public BackgroundJobRunner(
        IMcpSessionAccessor sessionAccessor,
        IBackgroundTaskTracker taskTracker,
        IUserNotificationService notificationService,
        ILogger<BackgroundJobRunner> logger)
    {
        _sessionAccessor = sessionAccessor;
        _taskTracker = taskTracker;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<BackgroundJobResult> RunAsync(IBackgroundJob job, McpSession session)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(session);

        // Store session for notifications
        _sessionAccessor.CurrentSession = session;

        _logger.LogInformation("Starting background job {JobType}: {DisplayName} (ID: {JobId})",
            job.JobType, job.DisplayName, job.JobId);

        // Start the job
        var startResult = await job.StartAsync();

        if (startResult.IsComplete)
        {
            // Job completed immediately (or failed to start)
            await SendNotificationAsync(job, startResult);
            return startResult;
        }

        // Track the job
        _taskTracker.Track(new TrackedTask
        {
            TaskId = job.JobId,
            JobType = job.JobType,
            DisplayName = job.DisplayName,
            Status = startResult.Status,
            StartedAt = startResult.StartedAt,
            Context = startResult.Context
        });

        // Monitor in background (fire and forget)
        _ = MonitorJobAsync(job);

        return startResult;
    }

    private async Task MonitorJobAsync(IBackgroundJob job)
    {
        var startTime = DateTime.UtcNow;

        _logger.LogDebug("Starting background monitoring for job {JobId}", job.JobId);

        try
        {
            while (DateTime.UtcNow - startTime < MaxJobDuration)
            {
                await Task.Delay(DefaultPollInterval);

                var result = await job.CheckStatusAsync();

                if (result.IsComplete)
                {
                    _taskTracker.Update(job.JobId, task =>
                    {
                        task.Status = result.Status;
                        task.CompletedAt = result.CompletedAt ?? DateTime.UtcNow;
                        task.FailureReason = result.ErrorMessage;
                    });

                    await SendNotificationAsync(job, result);

                    _logger.LogInformation("Job {JobId} completed with status {Status}",
                        job.JobId, result.Status);
                    return;
                }

                _logger.LogDebug("Job {JobId} still in progress, status={Status}",
                    job.JobId, result.Status);
            }

            // Timeout
            _logger.LogWarning("Job {JobId} timed out after {Duration}", job.JobId, MaxJobDuration);

            var timeoutResult = new BackgroundJobResult
            {
                IsComplete = true,
                IsSuccess = false,
                Status = "Timeout",
                ErrorMessage = $"Job did not complete within {MaxJobDuration.TotalHours} hours",
                StartedAt = startTime,
                CompletedAt = DateTime.UtcNow
            };

            await SendNotificationAsync(job, timeoutResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring job {JobId}", job.JobId);

            var errorResult = new BackgroundJobResult
            {
                IsComplete = true,
                IsSuccess = false,
                Status = "Error",
                ErrorMessage = $"Monitoring failed: {ex.Message}",
                StartedAt = startTime,
                CompletedAt = DateTime.UtcNow
            };

            await SendNotificationAsync(job, errorResult);
        }
    }

    private async Task SendNotificationAsync(IBackgroundJob job, BackgroundJobResult result)
    {
        try
        {
            var title = $"{job.JobType} {result.Status}";
            var duration = result.DurationFormatted ?? "unknown duration";

            if (result.IsSuccess)
            {
                var message = $"'{job.DisplayName}' completed successfully in {duration}";
                await _notificationService.NotifySuccessAsync(title, message);
            }
            else if (result.Status == "Timeout")
            {
                var message = $"'{job.DisplayName}' timed out";
                await _notificationService.NotifyWarningAsync(title, message);
            }
            else
            {
                var message = $"'{job.DisplayName}' failed: {result.ErrorMessage ?? "Unknown error"}";
                await _notificationService.NotifyErrorAsync(title, message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to send notification for job {JobId}", job.JobId);
        }
    }
}
