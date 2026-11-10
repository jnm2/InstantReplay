using System;
using System.Collections.Generic;
using Instrumental.InstantReplay;
using InstantReplaySamples;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace UnoSample;

public sealed partial class App : Application
{
    /// <summary>
    /// The renderer that Program.Main asked for, shown in the main window.
    /// </summary>
    public static string RendererName { get; set; } = "";

    private static string TargetDescription => $"Renderer: {RendererName}";

    private readonly List<Window> openWindows = new();
    private int windowCount;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        InstantReplayCamera.Start(reportBackgroundException: ex => dispatcherQueue.TryEnqueue(() => ShowException(ex)));

        CreateWindow().Activate();
    }

    private Window CreateWindow()
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
        openWindowButton.Click += (_, _) => CreateWindow().Activate();

        var panel = new StackPanel
        {
            Margin = new Thickness(24, 20, 24, 20),
            Children =
            {
                new TextBlock
                {
                    Text = isMainWindow ? "Uno Platform" : $"Window {number}",
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x5C, 0x2D, 0x91)),
                },
                new TextBlock { Text = SampleGif.Instructions, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 12) },
                new TextBox { Text = "Type here", Width = 300, HorizontalAlignment = HorizontalAlignment.Left },
                new Slider { Width = 300, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0), Maximum = 100, Value = 40 },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 4, 0, 0),
                    Children = { openWindowButton, saveButton },
                },
                status,
            },
        };

        if (isMainWindow)
        {
            panel.Children.Add(new TextBlock { Text = TargetDescription, FontSize = 12, Opacity = 0.7, Margin = new Thickness(0, 12, 0, 0) });
        }

        var root = new Grid
        {
            Background = new SolidColorBrush(isMainWindow ? Colors.White : Color.FromArgb(0xFF, 0xFF, 0xF4, 0xE0)),
            Children = { panel },
        };

        var window = new Window
        {
            Title = isMainWindow ? "Uno Platform sample" : $"Uno Platform sample: window {number}",
            Content = root,
        };

        // AppWindow sizes are in physical pixels.
        root.Loaded += (_, _) =>
        {
            var scale = root.XamlRoot?.RasterizationScale ?? 1;
            window.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = (int)(520 * scale), Height = (int)(420 * scale) });
        };

        openWindows.Add(window);
        window.Closed += (_, _) => openWindows.Remove(window);
        return window;
    }

    private async void ShowException(Exception exception)
    {
        if (openWindows.Count == 0) return;

        await new ContentDialog
        {
            Title = "Unhandled exception in Instrumental.InstantReplay",
            Content = new ScrollViewer { Content = new TextBlock { Text = exception.ToString(), TextWrapping = TextWrapping.Wrap } },
            CloseButtonText = "OK",
            XamlRoot = openWindows[0].Content?.XamlRoot,
        }.ShowAsync();
    }
}
