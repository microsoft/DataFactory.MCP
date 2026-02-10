using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;

namespace DataFactory.MCP.StdioUI;

/// <summary>
/// A borderless, topmost notification window positioned at the bottom-right of the screen.
/// Auto-closes after the configured duration.
/// </summary>
public class NotificationWindow : Window
{
    public NotificationWindow()
    {
        SystemDecorations = SystemDecorations.None;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        CanResize = false;

        var borderColor = TryParseColor(Program.BorderColor, Colors.DodgerBlue);

        Content = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            BorderBrush = new SolidColorBrush(borderColor),
            BorderThickness = new Thickness(1),
            MinWidth = 300,
            BoxShadow = new BoxShadows(new BoxShadow
            {
                Color = Color.Parse("#80000000"),
                Blur = 10,
                OffsetX = 0,
                OffsetY = 2
            }),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = $"{Program.Icon} {Program.Title}",
                        FontSize = 14,
                        FontWeight = FontWeight.Bold,
                        Foreground = new SolidColorBrush(borderColor),
                        Margin = new Thickness(0, 0, 0, 8)
                    },
                    new TextBlock
                    {
                        Text = Program.Message,
                        FontSize = 12,
                        Foreground = Brushes.White,
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 300
                    }
                }
            }
        };

        Opened += OnOpened;

        // Auto-close after duration
        DispatcherTimer.RunOnce(() =>
        {
            Close();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
        }, TimeSpan.FromMilliseconds(Program.DurationMs));
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        var screen = Screens.Primary;
        if (screen != null)
        {
            var scaling = screen.Scaling;
            var workArea = screen.WorkingArea;
            Position = new PixelPoint(
                (int)(workArea.Right - (Bounds.Width + 20) * scaling),
                (int)(workArea.Bottom - (Bounds.Height + 20) * scaling));
        }
    }

    private static Color TryParseColor(string colorStr, Color fallback)
    {
        try { return Color.Parse(colorStr); }
        catch { return fallback; }
    }
}
