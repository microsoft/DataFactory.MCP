using System.Diagnostics;
using System.Reflection;
using DataFactory.MCP.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Notifications;

/// <summary>
/// Cross-platform notification provider that shells out to the Avalonia StdioUI app.
/// Mirrors the Windows WPF toast design on all platforms.
/// </summary>
public class AvaloniaNotificationProvider : IPlatformNotificationProvider
{
    private readonly ILogger<AvaloniaNotificationProvider> _logger;
    private readonly string? _stdioUiDllPath;

    public AvaloniaNotificationProvider(ILogger<AvaloniaNotificationProvider> logger)
    {
        _logger = logger;
        _stdioUiDllPath = FindStdioUiDll();

        if (_stdioUiDllPath == null)
            _logger.LogWarning("DataFactory.MCP.StdioUI.dll not found. Avalonia notifications unavailable.");
        else
            _logger.LogDebug("Avalonia StdioUI DLL found at: {Path}", _stdioUiDllPath);
    }

    public bool IsSupported => _stdioUiDllPath != null;

    public Task ShowAsync(string title, string message, NotificationLevel level)
    {
        if (_stdioUiDllPath == null) return Task.CompletedTask;

        var (icon, borderColor) = level switch
        {
            NotificationLevel.Success => ("✅", "#28A745"),
            NotificationLevel.Error => ("❌", "#DC3545"),
            NotificationLevel.Warning => ("⚠️", "#FFC107"),
            _ => ("ℹ️", "#007ACC")
        };

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("exec");
            psi.ArgumentList.Add(_stdioUiDllPath);
            psi.ArgumentList.Add("--title");
            psi.ArgumentList.Add(title);
            psi.ArgumentList.Add("--message");
            psi.ArgumentList.Add(message);
            psi.ArgumentList.Add("--color");
            psi.ArgumentList.Add(borderColor);
            psi.ArgumentList.Add("--icon");
            psi.ArgumentList.Add(icon);

            Process.Start(psi);
            _logger.LogInformation("Notification launched: {Title} {Message}", title, message);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to show Avalonia toast notification");
        }

        return Task.CompletedTask;
    }

    private static string? FindStdioUiDll()
    {
        var assemblyDir = Path.GetDirectoryName(typeof(AvaloniaNotificationProvider).Assembly.Location);
        if (assemblyDir == null) return null;

        var path = Path.Combine(assemblyDir, "DataFactory.MCP.StdioUI.dll");
        return File.Exists(path) ? path : null;
    }
}
