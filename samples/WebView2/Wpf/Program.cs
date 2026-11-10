using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Instrumental.InstantReplay;
using InstantReplaySamples;
using Microsoft.Web.WebView2.Wpf;

internal static class Program
{
    private static int windowCount;

    [STAThread]
    private static void Main()
    {
        InstantReplayCamera.Start(reportBackgroundException: ex => MessageBox.Show(
            ex.ToString(),
            "Unhandled exception in Instrumental.InstantReplay",
            MessageBoxButton.OK,
            MessageBoxImage.Error));

        new Application().Run(CreateWindow());
    }

    private static Window CreateWindow()
    {
        var number = ++windowCount;
        var isMainWindow = number == 1;

        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) };

        var saveButton = new Button { Content = SampleGif.ButtonText, Padding = new Thickness(12, 4, 12, 4), Margin = new Thickness(8, 0, 0, 0) };
        saveButton.Click += async (_, _) =>
        {
            saveButton.IsEnabled = false;
            status.Text = "Saving…";
            status.Text = await SampleGif.SaveToDesktopAndOpenAsync();
            saveButton.IsEnabled = true;
        };

        var openWindowButton = new Button { Content = SampleGif.OpenWindowButtonText, Padding = new Thickness(12, 4, 12, 4) };
        openWindowButton.Click += (_, _) => CreateWindow().Show();

        // The standard WebView2 control is blank in recordings. WebView2CompositionControl draws the page as part of the WPF window.
        var webView = new WebView2CompositionControl
        {
            Source = new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "index.html")),
            Height = 170,
            Margin = new Thickness(0, 12, 0, 0),
        };

        return new Window
        {
            Title = isMainWindow ? "WebView2 in WPF sample" : $"WebView2 in WPF sample: window {number}",
            Width = 520,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = isMainWindow ? WindowStartupLocation.CenterScreen : WindowStartupLocation.Manual,
            Background = isMainWindow
                ? Brushes.White
                : new SolidColorBrush(Color.FromRgb(0xFF, 0xF4, 0xE0)),
            Content = new StackPanel
            {
                Margin = new Thickness(24, 20, 24, 20),
                Children =
                {
                    new TextBlock
                    {
                        Text = isMainWindow ? "WebView2 in WPF" : $"Window {number}",
                        FontSize = 24,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x5C, 0x2D, 0x91)),
                    },
                    new TextBlock { Text = SampleGif.Instructions, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 12) },
                    new TextBox { Text = "Type here", Width = 300, HorizontalAlignment = HorizontalAlignment.Left },
                    webView,
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
    }
}
