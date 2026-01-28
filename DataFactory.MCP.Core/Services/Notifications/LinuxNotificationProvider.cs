using System.Diagnostics;
using System.Runtime.InteropServices;
using DataFactory.MCP.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Services.Notifications;

/// <summary>
/// Linux notification provider using notify-send (libnotify).
/// Requires libnotify-bin package to be installed.
/// </summary>
public class LinuxNotificationProvider : IPlatformNotificationProvider
{
    private readonly ILogger<LinuxNotificationProvider> _logger;

    public LinuxNotificationProvider(ILogger<LinuxNotificationProvider> logger)
    {
        _logger = logger;
    }

    public bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    public Task ShowAsync(string title, string message, NotificationLevel level)
    {
        try
        {
            var urgency = level switch
            {
                NotificationLevel.Error => "critical",
                NotificationLevel.Warning => "normal",
                _ => "low"
            };

            Process.Start(new ProcessStartInfo
            {
                FileName = "notify-send",
                Arguments = $"-u {urgency} {Quote(title)} {Quote(message)}",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to show Linux notification");
        }

        return Task.CompletedTask;
    }

    private static string Quote(string s) =>
        "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
