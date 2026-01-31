using System.Threading.Channels;
using DataFactory.MCP.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Services.Notifications;

/// <summary>
/// Processes notifications sequentially with spacing to prevent overlap.
/// 
/// <para><b>Why a queue?</b></para>
/// <para>
/// When multiple background jobs complete around the same time, their notifications
/// would overlap if shown simultaneously. This queue ensures notifications are
/// displayed one at a time with a configurable delay between them.
/// </para>
/// 
/// <para><b>Why System.Threading.Channels?</b></para>
/// <para>
/// Channel provides a thread-safe async producer/consumer queue in a single API.
/// Without it, we'd need a List with manual locking PLUS a separate signaling
/// mechanism (AutoResetEvent/SemaphoreSlim) to wake the consumer when items arrive.
/// Channel combines both: thread-safe writes AND efficient async waiting.
/// </para>
/// <para>
/// Key benefit: The consumer uses <c>ReadAllAsync()</c> which sleeps when empty
/// and wakes automatically when items are added - no polling, no busy loops.
/// </para>
/// 
/// <para><b>Architecture:</b></para>
/// <list type="bullet">
///   <item>Uses System.Threading.Channels for efficient async producer/consumer pattern</item>
///   <item>Single background task processes notifications sequentially</item>
///   <item>Thread-safe: multiple producers can enqueue concurrently</item>
///   <item>Graceful shutdown: completes pending notifications before disposing</item>
/// </list>
/// 
/// <para><b>Flow:</b></para>
/// <code>
/// Producer(s)                    Consumer (single)
///     │                              │
///     ├── Enqueue(notification) ───► Channel ───► ProcessNotificationsAsync()
///     │                              │                    │
///     │                              │             Show notification
///     │                              │                    │
///     │                              │             Wait 3 seconds
///     │                              │                    │
///     │                              │             Next notification...
/// </code>
/// </summary>
public class NotificationQueue : INotificationQueue, IDisposable
{
    /// <summary>
    /// Delay between showing consecutive notifications.
    /// Prevents overlap when multiple jobs complete close together.
    /// </summary>
    private static readonly TimeSpan NotificationSpacing = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The Channel - a thread-safe async producer/consumer queue.
    /// 
    /// Why Channel instead of a simple Queue or List?
    /// - Thread-safe writes without explicit locking
    /// - Built-in async waiting (ReadAllAsync sleeps when empty, wakes on item added)
    /// - No busy loops or polling needed
    /// - Combines queue + signaling in one clean API
    /// 
    /// Unbounded because notifications are lightweight and we don't want to block producers.
    /// </summary>
    private readonly Channel<QueuedNotification> _channel;

    /// <summary>
    /// The actual notification service that shows notifications (platform-specific).
    /// </summary>
    private readonly IUserNotificationService _notificationService;

    private readonly ILogger<NotificationQueue> _logger;

    /// <summary>
    /// Background task that processes the queue.
    /// Started in constructor, runs until disposal.
    /// </summary>
    private readonly Task _processingTask;

    /// <summary>
    /// Cancellation token source for graceful shutdown.
    /// </summary>
    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// Thread-safe counter of pending notifications.
    /// Used to determine if spacing delay is needed.
    /// </summary>
    private int _pendingCount;

    public NotificationQueue(
        IUserNotificationService notificationService,
        ILogger<NotificationQueue> logger)
    {
        _notificationService = notificationService;
        _logger = logger;

        // Create unbounded channel with performance hints:
        // - SingleReader: true - Only one consumer (our processor task), enables optimizations
        // - SingleWriter: false - Multiple jobs can enqueue concurrently from different threads
        _channel = Channel.CreateUnbounded<QueuedNotification>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        // Start the background processor immediately.
        // It uses ReadAllAsync which is event-driven:
        // - Sleeps when queue is empty (no CPU usage)
        // - Wakes automatically when TryWrite adds an item
        // - Completes when Writer.Complete() is called
        _processingTask = ProcessNotificationsAsync(_cts.Token);
    }

    /// <summary>
    /// Adds a notification to the queue for processing.
    /// Thread-safe: can be called from multiple threads concurrently.
    /// </summary>
    /// <param name="notification">The notification to show</param>
    public void Enqueue(QueuedNotification notification)
    {
        // Increment pending count BEFORE writing to channel
        // This ensures count is accurate even if processor reads immediately
        Interlocked.Increment(ref _pendingCount);

        if (!_channel.Writer.TryWrite(notification))
        {
            // Should never happen with unbounded channel, but handle gracefully
            Interlocked.Decrement(ref _pendingCount);
            _logger.LogWarning("Failed to enqueue notification: {Title}", notification.Title);
        }
        else
        {
            _logger.LogDebug("Notification queued: {Title}, pending count: {Count}",
                notification.Title, _pendingCount);
        }
    }

    /// <summary>
    /// Gets the number of notifications waiting to be shown.
    /// </summary>
    public int PendingCount => _pendingCount;

    /// <summary>
    /// Background processor that reads from the channel and shows notifications.
    /// 
    /// Uses <c>ReadAllAsync</c> which is the magic of Channel:
    /// - When queue is empty: awaits internally (no CPU usage, no polling)
    /// - When item is added: wakes up immediately to process it
    /// - When channel is completed: exits the foreach cleanly
    /// 
    /// This is more efficient than polling with Task.Delay because it's event-driven.
    /// </summary>
    private async Task ProcessNotificationsAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Notification queue processor started");

        try
        {
            // ReadAllAsync is the key Channel feature:
            // - Yields items as they become available
            // - Awaits efficiently when queue is empty (no busy loop!)
            // - Completes when _channel.Writer.Complete() is called
            await foreach (var notification in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    _logger.LogDebug("Processing notification: {Title}", notification.Title);

                    // Delegate to platform-specific notification service
                    await _notificationService.NotifyAsync(
                        notification.Title,
                        notification.Message,
                        notification.Level);

                    // Decrement AFTER showing (notification is no longer pending)
                    Interlocked.Decrement(ref _pendingCount);

                    // Add spacing delay if more notifications are waiting
                    // This prevents overlapping toast notifications
                    if (_pendingCount > 0)
                    {
                        _logger.LogDebug("Waiting {Spacing} before next notification, {Count} pending",
                            NotificationSpacing, _pendingCount);
                        await Task.Delay(NotificationSpacing, cancellationToken);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Don't let one failed notification stop the processor
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

    /// <summary>
    /// Gracefully shuts down the notification queue.
    /// Signals the channel to complete and waits for pending notifications.
    /// </summary>
    public void Dispose()
    {
        // Signal that no more items will be written
        // This causes ReadAllAsync to complete after processing remaining items
        _channel.Writer.TryComplete();

        // Cancel the processor (interrupts any spacing delay)
        _cts.Cancel();

        try
        {
            // Wait briefly for processor to finish
            // Don't wait forever - we're shutting down
            _processingTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Expected when cancellation occurs during Wait
        }

        _cts.Dispose();
    }
}
