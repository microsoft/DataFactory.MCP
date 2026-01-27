using DataFactory.MCP.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using System.Text.Json;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for sending MCP notifications to the client using the logging mechanism.
/// Uses notifications/message to send arbitrary data to the client.
/// </summary>
/// <remarks>
/// This service follows the MCP SDK pattern where the session is passed to each method
/// rather than stored as a field. Tools receive McpServer (which inherits from McpSession)
/// as an auto-bound parameter and pass it through to this service.
/// </remarks>
public class McpNotificationService : IMcpNotificationService
{
    private readonly ILogger<McpNotificationService> _logger;

    public McpNotificationService(ILogger<McpNotificationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendNotificationAsync(McpSession session, string level, string logger, object data)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(level);
        ArgumentException.ThrowIfNullOrWhiteSpace(logger);

        try
        {
            _logger.LogDebug("Sending MCP notification: level={Level}, logger={Logger}", level, logger);

            // Serialize data to JsonElement as required by the protocol
            var dataJson = JsonSerializer.SerializeToElement(data);

            // Use the standard MCP notifications/message format (Logging spec)
            var notificationParams = new LoggingMessageNotificationParams
            {
                Level = ParseLogLevel(level),
                Logger = logger,
                Data = dataJson
            };

            await session.SendNotificationAsync(
                NotificationMethods.LoggingMessageNotification,
                notificationParams);

            _logger.LogInformation("Successfully sent MCP notification to logger '{Logger}'", logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send MCP notification: level={Level}, logger={Logger}", level, logger);
            // Don't throw - notifications are best-effort
        }
    }

    /// <inheritdoc />
    public Task SendInfoAsync(McpSession session, string logger, object data)
        => SendNotificationAsync(session, "info", logger, data);

    /// <inheritdoc />
    public Task SendErrorAsync(McpSession session, string logger, object data)
        => SendNotificationAsync(session, "error", logger, data);

    private static LoggingLevel ParseLogLevel(string level)
    {
        return level.ToLowerInvariant() switch
        {
            "debug" => LoggingLevel.Debug,
            "info" => LoggingLevel.Info,
            "notice" => LoggingLevel.Notice,
            "warning" => LoggingLevel.Warning,
            "error" => LoggingLevel.Error,
            "critical" => LoggingLevel.Critical,
            "alert" => LoggingLevel.Alert,
            "emergency" => LoggingLevel.Emergency,
            _ => LoggingLevel.Info
        };
    }
}
