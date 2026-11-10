using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using Instrumental.InstantReplay;
using InstantReplaySamples;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

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

        WinForms.Application.EnableVisualStyles();
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
            Title = isMainWindow ? "Windows Forms in WPF sample" : $"Windows Forms in WPF sample: window {number}",
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
                        Text = isMainWindow ? "Windows Forms in WPF" : $"Window {number}",
                        FontSize = 24,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x5C, 0x2D, 0x91)),
                    },
                    new TextBlock { Text = SampleGif.Instructions, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 12) },
                    new TextBox { Text = "Type here", Width = 300, HorizontalAlignment = HorizontalAlignment.Left },
                    new Slider { Width = 300, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0), Maximum = 100, Value = 40 },
                    new WindowsFormsHost
                    {
                        Height = 150,
                        Margin = new Thickness(0, 12, 0, 0),
                        // WindowsFormsHost maps its Background to the Windows Forms control's BackColor.
                        Background = new SolidColorBrush(Color.FromRgb(0xD9, 0xF0, 0xDF)),
                        Child = CreateWinFormsContent(),
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

    private static WinForms.Control CreateWinFormsContent()
    {
        var clickCount = 0;
        var button = new WinForms.Button { Text = "Click me", AutoSize = true };
        button.Click += (_, _) => button.Text = $"Clicked {++clickCount}×";

        var comboBox = new WinForms.ComboBox { Width = 200, DropDownStyle = WinForms.ComboBoxStyle.DropDownList, Items = { "Red", "Green", "Blue" } };
        comboBox.SelectedIndex = 0;

        return new WinForms.FlowLayoutPanel
        {
            FlowDirection = WinForms.FlowDirection.TopDown,
            WrapContents = false,
            Padding = new WinForms.Padding(14, 10, 14, 10),
            BorderStyle = WinForms.BorderStyle.FixedSingle,
            Controls =
            {
                new WinForms.Label
                {
                    Text = "Windows Forms content",
                    AutoSize = true,
                    Font = new Drawing.Font("Segoe UI", 15, Drawing.FontStyle.Bold),
                    ForeColor = Drawing.Color.FromArgb(0x1B, 0x5E, 0x20),
                },
                new WinForms.Label { Text = "Hosted in a WindowsFormsHost", AutoSize = true, ForeColor = Drawing.Color.DimGray, Margin = new WinForms.Padding(3, 0, 3, 8) },
                new WinForms.FlowLayoutPanel
                {
                    AutoSize = true,
                    Margin = WinForms.Padding.Empty,
                    Controls = { new WinForms.TextBox { Text = "Windows Forms text box", Width = 200 }, button },
                },
                comboBox,
            },
        };
    }
}
