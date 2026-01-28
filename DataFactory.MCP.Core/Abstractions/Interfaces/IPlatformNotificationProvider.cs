namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Platform-specific notification provider.
/// Each platform (Windows, macOS, Linux) has its own implementation.
/// </summary>
public interface IPlatformNotificationProvider
{
    /// <summary>
    /// Gets whether this provider is supported on the current platform.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Shows a notification using the platform's native mechanism.
    /// </summary>
    Task ShowAsync(string title, string message, NotificationLevel level);
}
