namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// A queue for organizing and spacing out notifications.
/// Prevents notification overlap when multiple background tasks complete close together.
/// </summary>
public interface INotificationQueue
{
    /// <summary>
    /// Enqueues a notification to be shown.
    /// Notifications are processed in order with a delay between them.
    /// </summary>
    /// <param name="notification">The notification to enqueue</param>
    void Enqueue(QueuedNotification notification);

    /// <summary>
    /// Gets the number of pending notifications in the queue.
    /// </summary>
    int PendingCount { get; }
}

/// <summary>
/// Represents a notification waiting to be shown.
/// </summary>
public record QueuedNotification
{
    /// <summary>
    /// The notification title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The notification message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The notification severity level.
    /// </summary>
    public NotificationLevel Level { get; init; } = NotificationLevel.Info;

    /// <summary>
    /// When the notification was queued (for ordering and timeout).
    /// </summary>
    public DateTime QueuedAt { get; init; } = DateTime.UtcNow;
}
