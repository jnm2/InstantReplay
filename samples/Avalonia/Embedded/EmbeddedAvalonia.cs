using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

internal static class EmbeddedAvalonia
{
    /// <summary>
    /// Initializes Avalonia without starting an Avalonia application lifetime, so that Windows Forms or WPF keeps running
    /// the app.
    /// </summary>
    public static void Initialize()
    {
        AppBuilder.Configure<EmbeddedAvaloniaApp>()
            .UsePlatformDetect()
            // By default, Avalonia presents through Windows.UI.Composition, which leaves nothing for InstantReplay to
            // capture, so the embedded content records as a blank rectangle. RedirectionSurface keeps GPU rendering but
            // presents frames through the window's own surface, which InstantReplay can capture.
            .With(new Win32PlatformOptions { CompositionMode = [Win32CompositionMode.RedirectionSurface] })
            .SetupWithoutStarting();
    }

    public static Control CreateContent(string hostDescription)
    {
        var clickCount = 0;
        var button = new Button { Content = "Click me" };
        button.Click += (_, _) => button.Content = $"Clicked {++clickCount}×";

        return new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xE4, 0xC8)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xE6, 0x51, 0x00)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Avalonia content",
                        FontSize = 20,
                        FontWeight = FontWeight.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xBF, 0x36, 0x0C)),
                    },
                    new TextBlock { Text = hostDescription, Foreground = Brushes.DimGray, TextWrapping = TextWrapping.Wrap },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        Children = { new TextBox { Text = "Avalonia text box", Width = 200 }, button },
                    },
                    new Slider { Width = 300, HorizontalAlignment = HorizontalAlignment.Left, Maximum = 100, Value = 60 },
                },
            },
        };
    }
}

internal sealed class EmbeddedAvaloniaApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        RequestedThemeVariant = ThemeVariant.Light;
    }
}
