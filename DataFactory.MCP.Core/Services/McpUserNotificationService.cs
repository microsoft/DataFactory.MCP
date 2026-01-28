using DataFactory.MCP.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using System.Text.Json;

namespace DataFactory.MCP.Services;

/// <summary>
/// User notification service that sends notifications via MCP protocol.
/// Uses the session accessor to get the current session.
/// Best for HTTP mode where OS toasts aren't available.
/// </summary>
public class McpUserNotificationService : IUserNotificationService
{
    private readonly IMcpSessionAccessor _sessionAccessor;
    private readonly ILogger<McpUserNotificationService> _logger;

    public McpUserNotificationService(
        IMcpSessionAccessor sessionAccessor,
        ILogger<McpUserNotificationService> logger)
    {
        _sessionAccessor = sessionAccessor;
        _logger = logger;
    }

    public async Task NotifyAsync(string title, string message, NotificationLevel level = NotificationLevel.Info)
    {
        var session = _sessionAccessor.CurrentSession;
        if (session == null)
        {
            _logger.LogWarning("Cannot send MCP notification - no active session. Title: {Title}", title);
            return;
        }

        try
        {
            var mcpLevel = level switch
            {
                NotificationLevel.Error => LoggingLevel.Error,
                NotificationLevel.Warning => LoggingLevel.Warning,
                _ => LoggingLevel.Info
            };

            var data = new
            {
                title,
                message,
                level = level.ToString().ToLowerInvariant(),
                timestamp = DateTime.UtcNow.ToString("o")
            };

            var notificationParams = new LoggingMessageNotificationParams
            {
                Level = mcpLevel,
                Logger = "UserNotification",
                Data = JsonSerializer.SerializeToElement(data)
            };

            await session.SendNotificationAsync(
                NotificationMethods.LoggingMessageNotification,
                notificationParams);

            _logger.LogDebug("Sent MCP user notification: {Title}", title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send MCP notification: {Title}", title);
            // Don't throw - notifications are best-effort
        }
    }
}
