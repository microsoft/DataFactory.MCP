using System.Collections.Concurrent;
using DataFactory.MCP.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;

namespace DataFactory.MCP.Services.BackgroundTasks;

/// <summary>
/// Manages the complete lifecycle of background jobs: start, track, monitor, and notify.
/// Uses a single timer-based polling loop for efficiency.
/// Thread-safe for concurrent operations.
/// </summary>
public class BackgroundJobMonitor : IBackgroundJobMonitor, IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan MaxJobAge = TimeSpan.FromHours(4);

    private readonly ConcurrentDictionary<string, MonitoredJob> _activeJobs = new();
    private readonly ConcurrentDictionary<string, TrackedTask> _allTasks = new();
    private readonly IMcpSessionAccessor _sessionAccessor;
    private readonly INotificationQueue _notificationQueue;
    private readonly ILogger<BackgroundJobMonitor> _logger;
    private readonly Timer _pollTimer;
    private readonly SemaphoreSlim _pollLock = new(1, 1);
    private bool _disposed;

    public BackgroundJobMonitor(
        IMcpSessionAccessor sessionAccessor,
        INotificationQueue notificationQueue,
        ILogger<BackgroundJobMonitor> logger)
    {
        _sessionAccessor = sessionAccessor;
        _notificationQueue = notificationQueue;
        _logger = logger;

        // Start timer but don't poll until we have jobs
        _pollTimer = new Timer(OnPollTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);

        _logger.LogDebug("BackgroundJobMonitor initialized with {Interval}s poll interval",
            PollInterval.TotalSeconds);
    }

    public async Task<BackgroundJobResult> StartJobAsync(IBackgroundJob job, McpSession session)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(session);

        // Store session for notifications
        _sessionAccessor.CurrentSession = session;

        _logger.LogInformation("Starting background job {JobType}: {DisplayName} (ID: {JobId})",
            job.JobType, job.DisplayName, job.JobId);

        // Start the job
        var startResult = await job.StartAsync();

        // Track the task
        var trackedTask = new TrackedTask
        {
            TaskId = job.JobId,
            JobType = job.JobType,
            DisplayName = job.DisplayName,
            Status = startResult.Status,
            StartedAt = startResult.StartedAt,
            Context = startResult.Context
        };
        _allTasks[job.JobId] = trackedTask;

        if (startResult.IsComplete)
        {
            // Job completed immediately (or failed to start)
            trackedTask.Status = startResult.Status;
            trackedTask.CompletedAt = startResult.CompletedAt ?? DateTime.UtcNow;
            trackedTask.FailureReason = startResult.ErrorMessage;

            EnqueueNotification(job, startResult);
            return startResult;
        }

        // Register for monitoring
        var monitoredJob = new MonitoredJob
        {
            Job = job,
            RegisteredAt = DateTime.UtcNow
        };

        if (_activeJobs.TryAdd(job.JobId, monitoredJob))
        {
            _logger.LogDebug("Job {JobId} registered for monitoring. Active jobs: {Count}",
                job.JobId, _activeJobs.Count);

            // Start polling if this is the first job
            if (_activeJobs.Count == 1)
            {
                StartPolling();
            }
        }

        return startResult;
    }

    public TrackedTask? GetTask(string taskId)
    {
        return _allTasks.TryGetValue(taskId, out var task) ? task : null;
    }

    public IReadOnlyList<TrackedTask> GetAllTasks()
    {
        return _allTasks.Values.ToList().AsReadOnly();
    }

    public bool HasActiveJobs => !_activeJobs.IsEmpty;

    public int ActiveJobCount => _activeJobs.Count;

    private void StartPolling()
    {
        _logger.LogDebug("Starting poll timer");
        _pollTimer.Change(PollInterval, PollInterval);
    }

    private void StopPolling()
    {
        _logger.LogDebug("Stopping poll timer (no active jobs)");
        _pollTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private async void OnPollTimerElapsed(object? state)
    {
        if (_disposed) return;

        // Prevent concurrent poll executions
        if (!await _pollLock.WaitAsync(0))
        {
            _logger.LogDebug("Poll already in progress, skipping");
            return;
        }

        try
        {
            await PollAllJobsAsync();
        }
        finally
        {
            _pollLock.Release();
        }
    }

    private async Task PollAllJobsAsync()
    {
        if (_activeJobs.IsEmpty)
        {
            StopPolling();
            return;
        }

        _logger.LogDebug("Polling {Count} active job(s)", _activeJobs.Count);

        var now = DateTime.UtcNow;

        // Check all jobs in parallel
        var checkTasks = _activeJobs.Values.Select(async monitoredJob =>
        {
            try
            {
                // Check for timeout
                if (now - monitoredJob.RegisteredAt > MaxJobAge)
                {
                    _logger.LogWarning("Job {JobId} timed out after {Duration}",
                        monitoredJob.Job.JobId, MaxJobAge);

                    HandleJobCompletion(monitoredJob.Job, new BackgroundJobResult
                    {
                        IsComplete = true,
                        IsSuccess = false,
                        Status = "Timeout",
                        ErrorMessage = $"Job did not complete within {MaxJobAge.TotalHours} hours",
                        StartedAt = monitoredJob.RegisteredAt,
                        CompletedAt = now
                    });

                    return monitoredJob.Job.JobId;
                }

                var result = await monitoredJob.Job.CheckStatusAsync();

                if (result.IsComplete)
                {
                    _logger.LogInformation("Job {JobId} completed with status {Status}",
                        monitoredJob.Job.JobId, result.Status);

                    HandleJobCompletion(monitoredJob.Job, result);
                    return monitoredJob.Job.JobId;
                }

                _logger.LogDebug("Job {JobId} still in progress: {Status}",
                    monitoredJob.Job.JobId, result.Status);

                return null; // Not completed
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking job {JobId}", monitoredJob.Job.JobId);

                HandleJobCompletion(monitoredJob.Job, new BackgroundJobResult
                {
                    IsComplete = true,
                    IsSuccess = false,
                    Status = "Error",
                    ErrorMessage = $"Monitoring failed: {ex.Message}",
                    StartedAt = monitoredJob.RegisteredAt,
                    CompletedAt = now
                });

                return monitoredJob.Job.JobId;
            }
        });

        var results = await Task.WhenAll(checkTasks);

        // Remove completed jobs from active monitoring
        foreach (var jobId in results.Where(id => id != null))
        {
            _activeJobs.TryRemove(jobId!, out _);
        }

        // Stop timer if no more jobs
        if (_activeJobs.IsEmpty)
        {
            StopPolling();
        }
    }

    private void HandleJobCompletion(IBackgroundJob job, BackgroundJobResult result)
    {
        // Update tracked task
        if (_allTasks.TryGetValue(job.JobId, out var task))
        {
            task.Status = result.Status;
            task.CompletedAt = result.CompletedAt ?? DateTime.UtcNow;
            task.FailureReason = result.ErrorMessage;
        }

        // Queue notification
        EnqueueNotification(job, result);
    }

    private void EnqueueNotification(IBackgroundJob job, BackgroundJobResult result)
    {
        var title = $"{job.JobType} {result.Status}";
        var duration = result.DurationFormatted ?? "unknown duration";

        QueuedNotification notification;

        if (result.IsSuccess)
        {
            notification = new QueuedNotification
            {
                Title = title,
                Message = $"'{job.DisplayName}' completed successfully in {duration}",
                Level = NotificationLevel.Success
            };
        }
        else if (result.Status == "Timeout")
        {
            notification = new QueuedNotification
            {
                Title = title,
                Message = $"'{job.DisplayName}' timed out",
                Level = NotificationLevel.Warning
            };
        }
        else
        {
            notification = new QueuedNotification
            {
                Title = title,
                Message = $"'{job.DisplayName}' failed: {result.ErrorMessage ?? "Unknown error"}",
                Level = NotificationLevel.Error
            };
        }

        _notificationQueue.Enqueue(notification);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _pollTimer.Dispose();
        _pollLock.Dispose();
    }

    private class MonitoredJob
    {
        public required IBackgroundJob Job { get; init; }
        public required DateTime RegisteredAt { get; init; }
    }
}
