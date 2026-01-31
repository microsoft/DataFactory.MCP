using DataFactory.MCP.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Services;

/// <summary>
/// User notification service that delegates to platform-specific providers.
/// Follows Open/Closed Principle - add new platforms by adding new providers.
/// </summary>
public class SystemToastNotificationService : IUserNotificationService
{
    private readonly IPlatformNotificationProvider? _provider;
    private readonly ILogger<SystemToastNotificationService> _logger;

    public SystemToastNotificationService(
        IEnumerable<IPlatformNotificationProvider> providers,
        ILogger<SystemToastNotificationService> logger)
    {
        // Select the first supported provider for the current platform
        _provider = providers.FirstOrDefault(p => p.IsSupported);
        _logger = logger;

        if (_provider == null)
        {
            _logger.LogWarning("No supported notification provider found for this platform");
        }
        else
        {
            _logger.LogDebug("Using notification provider: {Provider}", _provider.GetType().Name);
        }
    }

    public async Task NotifyAsync(string title, string message, NotificationLevel level = NotificationLevel.Info)
    {
        if (_provider == null)
        {
            _logger.LogWarning("No notification provider available, skipping notification: {Title} {Message}", title, message);
            return;
        }

        try
        {
            await _provider.ShowAsync(title, message, level);
            _logger.LogInformation("Notification shown via {Provider}: {Title} {Message}", _provider.GetType().Name, title, message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to show notification: {Title} {Message}", title, message);
        }
    }
}
