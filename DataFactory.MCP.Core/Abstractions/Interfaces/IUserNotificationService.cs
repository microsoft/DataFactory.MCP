namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Service for notifying the user about events (e.g., background task completion).
/// Implementations vary by transport: OS toasts for stdio, logging for HTTP.
/// </summary>
public interface IUserNotificationService
{
    /// <summary>
    /// Sends a notification to the user.
    /// </summary>
    /// <param name="title">The notification title</param>
    /// <param name="message">The notification message</param>
    /// <param name="level">The notification level (Info, Success, Warning, Error)</param>
    Task NotifyAsync(string title, string message, NotificationLevel level = NotificationLevel.Info);
}

/// <summary>
/// Notification severity level
/// </summary>
public enum NotificationLevel
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// Extension methods for IUserNotificationService
/// </summary>
public static class UserNotificationExtensions
{
    public static Task NotifySuccessAsync(this IUserNotificationService service, string title, string message)
        => service.NotifyAsync(title, message, NotificationLevel.Success);

    public static Task NotifyWarningAsync(this IUserNotificationService service, string title, string message)
        => service.NotifyAsync(title, message, NotificationLevel.Warning);

    public static Task NotifyErrorAsync(this IUserNotificationService service, string title, string message)
        => service.NotifyAsync(title, message, NotificationLevel.Error);
}
