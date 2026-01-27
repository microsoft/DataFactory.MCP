using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace DataFactory.WindowsMCP.Tools;

/// <summary>
/// Windows-specific tool for retrieving system information using Windows Registry.
/// This tool demonstrates Windows-only functionality that cannot run cross-platform.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public class WindowsSystemInfoTool
{
    [McpServerTool, Description("Get Windows system information from the registry")]
    public string GetWindowsSystemInfo()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key == null)
                return "Unable to read Windows system information.";

            var productName = key.GetValue("ProductName")?.ToString() ?? "Unknown";
            var displayVersion = key.GetValue("DisplayVersion")?.ToString() ?? "Unknown";
            var buildNumber = key.GetValue("CurrentBuildNumber")?.ToString() ?? "Unknown";
            var registeredOwner = key.GetValue("RegisteredOwner")?.ToString() ?? "Unknown";

            return $"""
                Windows System Information:
                - Product: {productName}
                - Version: {displayVersion}
                - Build: {buildNumber}
                - Owner: {registeredOwner}
                """;
        }
        catch (Exception ex)
        {
            return $"Error reading system info: {ex.Message}";
        }
    }

    [McpServerTool, Description("Get installed .NET runtimes from Windows registry")]
    public string GetInstalledDotNetVersions()
    {
        try
        {
            var result = new List<string> { "Installed .NET Versions:" };

            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost");
            if (key != null)
            {
                var version = key.GetValue("Version")?.ToString();
                if (!string.IsNullOrEmpty(version))
                    result.Add($"- .NET Host: {version}");
            }

            return result.Count > 1 
                ? string.Join("\n", result) 
                : "No .NET installation information found in registry.";
        }
        catch (Exception ex)
        {
            return $"Error reading .NET versions: {ex.Message}";
        }
    }
}
