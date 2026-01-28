using System.Collections.Concurrent;
using System.Threading.Channels;
using DataFactory.MCP.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Services.Notifications;

/// <summary>
/// Processes notifications in order with spacing to prevent overlap.
/// Uses a Channel for efficient async producer/consumer pattern.
/// </summary>
public class NotificationQueue : INotificationQueue, IDisposable
{
    // Delay between showing notifications to prevent overlap
    private static readonly TimeSpan NotificationSpacing = TimeSpan.FromSeconds(3);

    private readonly Channel<QueuedNotification> _channel;
    private readonly IUserNotificationService _notificationService;
    private readonly ILogger<NotificationQueue> _logger;
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _cts = new();
    private int _pendingCount;

    public NotificationQueue(
        IUserNotificationService notificationService,
        ILogger<NotificationQueue> logger)
    {
        _notificationService = notificationService;
        _logger = logger;

        // Unbounded channel - notifications are lightweight
        _channel = Channel.CreateUnbounded<QueuedNotification>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        // Start the background processor
        _processingTask = ProcessNotificationsAsync(_cts.Token);
    }

    public void Enqueue(QueuedNotification notification)
    {
        Interlocked.Increment(ref _pendingCount);

        if (!_channel.Writer.TryWrite(notification))
        {
            Interlocked.Decrement(ref _pendingCount);
            _logger.LogWarning("Failed to enqueue notification: {Title}", notification.Title);
        }
        else
        {
            _logger.LogDebug("Notification queued: {Title}, pending count: {Count}",
                notification.Title, _pendingCount);
        }
    }

    public int PendingCount => _pendingCount;

    private async Task ProcessNotificationsAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Notification queue processor started");

        try
        {
            await foreach (var notification in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    _logger.LogDebug("Processing notification: {Title}", notification.Title);

                    await _notificationService.NotifyAsync(
                        notification.Title,
                        notification.Message,
                        notification.Level);

                    Interlocked.Decrement(ref _pendingCount);

                    // Add spacing before next notification (if more are pending)
                    if (_pendingCount > 0)
                    {
                        _logger.LogDebug("Waiting {Spacing} before next notification, {Count} pending",
                            NotificationSpacing, _pendingCount);
                        await Task.Delay(NotificationSpacing, cancellationToken);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Failed to show notification: {Title}", notification.Title);
                    Interlocked.Decrement(ref _pendingCount);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Notification queue processor cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification queue processor failed");
        }
    }

    public void Dispose()
    {
        _channel.Writer.TryComplete();
        _cts.Cancel();

        try
        {
            _processingTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Expected during shutdown
        }

        _cts.Dispose();
    }
}
