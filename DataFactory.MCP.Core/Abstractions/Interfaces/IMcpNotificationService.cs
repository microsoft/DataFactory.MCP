using ModelContextProtocol;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Service for sending MCP notifications to the client.
/// Uses the MCP Logging/Message notification mechanism for background task completion.
/// </summary>
/// <remarks>
/// This service accepts an <see cref="McpSession"/> parameter in each method rather than
/// storing it as a field. This follows the MCP SDK pattern where the session is passed
/// from tools (which receive McpServer as an auto-bound parameter, and McpServer inherits from McpSession).
/// </remarks>
public interface IMcpNotificationService
{
    /// <summary>
    /// Sends a notification message to the MCP client
    /// </summary>
    /// <param name="session">The MCP session to send the notification through</param>
    /// <param name="level">Log level: debug, info, notice, warning, error, critical, alert, emergency</param>
    /// <param name="logger">Logger name for categorization (e.g., "BackgroundTasks")</param>
    /// <param name="data">Arbitrary data to include in the notification</param>
    Task SendNotificationAsync(McpSession session, string level, string logger, object data);

    /// <summary>
    /// Sends an info-level notification
    /// </summary>
    Task SendInfoAsync(McpSession session, string logger, object data);

    /// <summary>
    /// Sends an error-level notification
    /// </summary>
    Task SendErrorAsync(McpSession session, string logger, object data);
}
