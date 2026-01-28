using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using DataFactory.MCP.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Services.Notifications;

/// <summary>
/// Windows notification provider using WPF toast via PowerShell.
/// </summary>
public class WindowsToastNotificationProvider : IPlatformNotificationProvider
{
    private readonly ILogger<WindowsToastNotificationProvider> _logger;
    private readonly string _xamlTemplate;
    private readonly string _psScriptTemplate;

    public WindowsToastNotificationProvider(ILogger<WindowsToastNotificationProvider> logger)
    {
        _logger = logger;
        _xamlTemplate = LoadEmbeddedResource("ToastNotification.xaml");
        _psScriptTemplate = LoadEmbeddedResource("ToastNotification.ps1");
    }

    public bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public Task ShowAsync(string title, string message, NotificationLevel level)
    {
        var icon = level switch
        {
            NotificationLevel.Success => "✅",
            NotificationLevel.Error => "❌",
            NotificationLevel.Warning => "⚠️",
            _ => "ℹ️"
        };

        var borderColor = level switch
        {
            NotificationLevel.Success => "#28A745",
            NotificationLevel.Error => "#DC3545",
            NotificationLevel.Warning => "#FFC107",
            _ => "#007ACC"
        };

        var safeTitle = EscapeXml($"{icon} {title}");
        var safeMessage = EscapeXml(message);

        var xamlContent = _xamlTemplate
            .Replace("{{BorderColor}}", borderColor)
            .Replace("{{Title}}", safeTitle)
            .Replace("{{Message}}", safeMessage);

        var psScript = _psScriptTemplate.Replace("{{XamlContent}}", xamlContent);

        try
        {
            var scriptPath = Path.Combine(Path.GetTempPath(), $"mcp-toast-{Guid.NewGuid():N}.ps1");
            File.WriteAllText(scriptPath, psScript, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -STA -WindowStyle Hidden -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            _ = Task.Delay(10000).ContinueWith(_ =>
            {
                try { File.Delete(scriptPath); } catch { }
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to show Windows toast notification");
        }

        return Task.CompletedTask;
    }

    private static string LoadEmbeddedResource(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"DataFactory.MCP.Core.Resources.{fileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string EscapeXml(string s) =>
        s.Replace("&", "&amp;")
         .Replace("<", "&lt;")
         .Replace(">", "&gt;")
         .Replace("\"", "&quot;")
         .Replace("'", "&apos;");
}
