using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Instrumental.InstantReplay;
using InstantReplaySamples;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        InstantReplayCamera.Start(reportBackgroundException: ex => Dispatcher.UIThread.Post(() => new Window
        {
            Title = "Unhandled exception in Instrumental.InstantReplay",
            Width = 700,
            Height = 400,
            Content = new TextBox { Text = ex.ToString(), IsReadOnly = true, TextWrapping = TextWrapping.Wrap },
        }.Show()));

        var builder = AppBuilder.Configure<SampleApp>().UsePlatformDetect();

        if (args.Contains("--software"))
        {
            builder = builder.With(new Win32PlatformOptions { RenderingMode = [Win32RenderingMode.Software] });
            SampleApp.Mode = "RenderingMode = Software";
        }
        else
        {
            // By default, Avalonia presents windows through Windows.UI.Composition, which leaves nothing for
            // InstantReplay to capture, so it records black frames. RedirectionSurface keeps GPU rendering but presents
            // frames through the window's own surface, which InstantReplay can capture.
            builder = builder.With(new Win32PlatformOptions { CompositionMode = [Win32CompositionMode.RedirectionSurface] });
            SampleApp.Mode = "CompositionMode = RedirectionSurface";
        }

        return builder.StartWithClassicDesktopLifetime(args);
    }
}

internal sealed class SampleApp : Application
{
    private static int windowCount;

    public static string Mode { get; set; } = "";

    private static string FrameworkName => "Avalonia " + typeof(AppBuilder).Assembly.GetName().Version!.Major;

    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        RequestedThemeVariant = ThemeVariant.Light;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = CreateWindow();

        base.OnFrameworkInitializationCompleted();
    }

    private static Window CreateWindow()
    {
        var number = ++windowCount;
        var isMainWindow = number == 1;

        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) };

        var saveButton = new Button { Content = SampleGif.ButtonText, Margin = new Thickness(8, 0, 0, 0) };
        saveButton.Click += async (_, _) =>
        {
            saveButton.IsEnabled = false;
            status.Text = "Saving…";
            status.Text = await SampleGif.SaveToDesktopAndOpenAsync();
            saveButton.IsEnabled = true;
        };

        var openWindowButton = new Button { Content = SampleGif.OpenWindowButtonText };

        var window = new Window
        {
            Title = isMainWindow ? $"{FrameworkName} sample" : $"{FrameworkName} sample: window {number}",
            Width = 520,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = isMainWindow ? WindowStartupLocation.CenterScreen : WindowStartupLocation.Manual,
            Background = new SolidColorBrush(isMainWindow ? Colors.White : Color.FromRgb(0xFF, 0xF4, 0xE0)),
            Content = new StackPanel
            {
                Margin = new Thickness(24, 20, 24, 20),
                Children =
                {
                    new TextBlock
                    {
                        Text = isMainWindow ? FrameworkName : $"Window {number}",
                        FontSize = 24,
                        FontWeight = FontWeight.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x5C, 0x2D, 0x91)),
                    },
                    new TextBlock { Text = "Mode: " + Mode, FontSize = 12, Foreground = Brushes.DimGray, TextWrapping = TextWrapping.Wrap },
                    new TextBlock { Text = SampleGif.Instructions, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 12) },
                    new TextBox { Text = "Type here", Width = 300, HorizontalAlignment = HorizontalAlignment.Left },
                    new Slider { Width = 300, HorizontalAlignment = HorizontalAlignment.Left, Maximum = 100, Value = 40 },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(0, 12, 0, 0),
                        Children = { openWindowButton, saveButton },
                    },
                    status,
                },
            },
        };

        openWindowButton.Click += (_, _) =>
        {
            var newWindow = CreateWindow();
            newWindow.Position = window.Position + new PixelPoint(40, 40);
            newWindow.Show();
        };

        return window;
    }
}
