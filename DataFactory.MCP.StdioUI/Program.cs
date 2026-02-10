using Avalonia;

namespace DataFactory.MCP.StdioUI;

public static class Program
{
    internal static string Title { get; private set; } = "Notification";
    internal static string Message { get; private set; } = "";
    internal static string BorderColor { get; private set; } = "#007ACC";
    internal static string Icon { get; private set; } = "ℹ️";
    internal static int DurationMs { get; private set; } = 5000;

    [STAThread]
    public static void Main(string[] args)
    {
        Title = GetArg(args, "--title") ?? Title;
        Message = GetArg(args, "--message") ?? Message;
        BorderColor = GetArg(args, "--color") ?? BorderColor;
        Icon = GetArg(args, "--icon") ?? Icon;
        if (int.TryParse(GetArg(args, "--duration"), out var d)) DurationMs = d;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(Array.Empty<string>());
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<StdioUIApp>()
            .UsePlatformDetect()
            .LogToTrace();

    private static string? GetArg(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
