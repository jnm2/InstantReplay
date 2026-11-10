using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Instrumental.InstantReplay;
using InstantReplaySamples;

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
        saveButton.Click += (_, _) =>
        {
            saveButton.IsEnabled = false;
            status.Text = "Saving…";
            SampleGif.SaveToDesktopAndOpenInBackground(message => saveButton.Dispatcher.BeginInvoke(new Action(() =>
            {
                status.Text = message;
                saveButton.IsEnabled = true;
            })));
        };

        var openWindowButton = new Button { Content = SampleGif.OpenWindowButtonText, Padding = new Thickness(12, 4, 12, 4) };
        openWindowButton.Click += (_, _) => CreateWindow().Show();

        return new Window
        {
            Title = isMainWindow ? "WPF sample" : $"WPF sample: window {number}",
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
                        Text = isMainWindow ? "WPF" : $"Window {number}",
                        FontSize = 24,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x5C, 0x2D, 0x91)),
                    },
                    new TextBlock { Text = SampleGif.Instructions, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 12) },
                    new TextBox { Text = "Type here", Width = 300, HorizontalAlignment = HorizontalAlignment.Left },
                    new Slider { Width = 300, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0), Maximum = 100, Value = 40 },
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
