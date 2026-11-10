using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using Avalonia.Win32.Interoperability;
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

        EmbeddedAvalonia.Initialize();
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

        return new Window
        {
            Title = isMainWindow ? "Avalonia in WPF sample" : $"Avalonia in WPF sample: window {number}",
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
                        Text = isMainWindow ? "Avalonia in WPF" : $"Window {number}",
                        FontSize = 24,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x5C, 0x2D, 0x91)),
                    },
                    new TextBlock { Text = SampleGif.Instructions, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 12) },
                    new TextBox { Text = "Type here", Width = 300, HorizontalAlignment = HorizontalAlignment.Left },
                    new Slider { Width = 300, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0), Maximum = 100, Value = 40 },
                    // Avalonia has no WPF host control. Its maintainers recommend nesting its Windows Forms host in a
                    // WindowsFormsHost: https://github.com/AvaloniaUI/Avalonia/discussions/19715
                    new WindowsFormsHost
                    {
                        Height = 170,
                        Margin = new Thickness(0, 12, 0, 0),
                        Child = new WinFormsAvaloniaControlHost
                        {
                            Content = EmbeddedAvalonia.CreateContent("Hosted in a WinFormsAvaloniaControlHost inside a WindowsFormsHost"),
                        },
                    },
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
