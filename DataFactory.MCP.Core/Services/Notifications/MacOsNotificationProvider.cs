using System.Diagnostics;
using System.Runtime.InteropServices;
using DataFactory.MCP.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Services.Notifications;

/// <summary>
/// macOS notification provider using osascript (AppleScript).
/// </summary>
public class MacOsNotificationProvider : IPlatformNotificationProvider
{
    private readonly ILogger<MacOsNotificationProvider> _logger;

    public MacOsNotificationProvider(ILogger<MacOsNotificationProvider> logger)
    {
        _logger = logger;
    }

    public bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public Task ShowAsync(string title, string message, NotificationLevel level)
    {
        try
        {
            var escapedTitle = EscapeAppleScript(title);
            var escapedMessage = EscapeAppleScript(message);

            Process.Start(new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = $"-e \"display notification \\\"{escapedMessage}\\\" with title \\\"{escapedTitle}\\\"\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to show macOS notification");
        }

        return Task.CompletedTask;
    }

    private static string EscapeAppleScript(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
