using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace DataFactory.MCP.StdioUI;

public class StdioUIApp : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var window = new NotificationWindow();
            window.Show();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
