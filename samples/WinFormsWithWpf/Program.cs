using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Instrumental.InstantReplay;
using InstantReplaySamples;
using Wpf = System.Windows;
using WpfControls = System.Windows.Controls;
using WpfMedia = System.Windows.Media;

internal static class Program
{
    private static int windowCount;

    [STAThread]
    private static void Main()
    {
        InstantReplayCamera.Start(reportBackgroundException: ex => MessageBox.Show(
            ex.ToString(),
            "Unhandled exception in Instrumental.InstantReplay",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error));

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(CreateWindow());
    }

    private static Form CreateWindow()
    {
        var number = ++windowCount;
        var isMainWindow = number == 1;

        var status = new Label { AutoSize = true, MaximumSize = new Size(440, 0), Margin = new Padding(0, 12, 0, 0) };

        var saveButton = new Button { Text = SampleGif.ButtonText, AutoSize = true };
        saveButton.Click += (_, _) =>
        {
            saveButton.Enabled = false;
            status.Text = "Saving…";
            SampleGif.SaveToDesktopAndOpenInBackground(message => saveButton.BeginInvoke(new MethodInvoker(() =>
            {
                status.Text = message;
                saveButton.Enabled = true;
            })));
        };

        var openWindowButton = new Button { Text = SampleGif.OpenWindowButtonText, AutoSize = true };
        openWindowButton.Click += (_, _) => CreateWindow().Show();

        var form = new Form
        {
            Text = isMainWindow ? "WPF in Windows Forms sample" : $"WPF in Windows Forms sample: window {number}",
            AutoScaleMode = AutoScaleMode.Dpi,
            AutoScaleDimensions = new SizeF(96, 96),
            ClientSize = new Size(500, 540),
            StartPosition = isMainWindow ? FormStartPosition.CenterScreen : FormStartPosition.WindowsDefaultLocation,
            BackColor = isMainWindow ? Color.White : Color.FromArgb(0xFF, 0xF4, 0xE0),
        };

        form.Controls.Add(new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(24, 20, 24, 20),
            Controls =
            {
                new Label
                {
                    Text = isMainWindow ? "WPF in Windows Forms" : $"Window {number}",
                    AutoSize = true,
                    Font = new Font(form.Font.FontFamily, 18, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0x5C, 0x2D, 0x91),
                },
                new Label { Text = SampleGif.Instructions, AutoSize = true, MaximumSize = new Size(440, 0), Margin = new Padding(0, 8, 0, 12) },
                new TextBox { Width = 300, Text = "Type here" },
                CreateComboBox(),
                new ElementHost { Size = new Size(452, 170), Margin = new Padding(0, 12, 0, 0), Child = CreateWpfContent() },
                new FlowLayoutPanel
                {
                    AutoSize = true,
                    Margin = new Padding(0, 12, 0, 0),
                    Controls = { openWindowButton, saveButton },
                },
                status,
            },
        });

        return form;
    }

    private static ComboBox CreateComboBox()
    {
        var comboBox = new ComboBox { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, Items = { "Red", "Green", "Blue" } };
        comboBox.SelectedIndex = 0;
        return comboBox;
    }

    private static Wpf.UIElement CreateWpfContent()
    {
        var clickCount = 0;
        var button = new WpfControls.Button { Content = "Click me", Padding = new Wpf.Thickness(12, 4, 12, 4), Margin = new Wpf.Thickness(8, 0, 0, 0) };
        button.Click += (_, _) => button.Content = $"Clicked {++clickCount}×";

        return new WpfControls.Border
        {
            Background = new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(0xD4, 0xEF, 0xDB)),
            BorderBrush = new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(0x2E, 0x7D, 0x32)),
            BorderThickness = new Wpf.Thickness(2),
            CornerRadius = new Wpf.CornerRadius(8),
            Padding = new Wpf.Thickness(16, 12, 16, 12),
            Child = new WpfControls.StackPanel
            {
                Children =
                {
                    new WpfControls.TextBlock
                    {
                        Text = "WPF content",
                        FontSize = 20,
                        FontWeight = Wpf.FontWeights.Bold,
                        Foreground = new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(0x1B, 0x5E, 0x20)),
                    },
                    new WpfControls.TextBlock { Text = "Hosted in an ElementHost", Foreground = WpfMedia.Brushes.DimGray, Margin = new Wpf.Thickness(0, 0, 0, 8) },
                    new WpfControls.StackPanel
                    {
                        Orientation = WpfControls.Orientation.Horizontal,
                        Children =
                        {
                            new WpfControls.TextBox { Text = "WPF text box", Width = 200 },
                            button,
                        },
                    },
                    new WpfControls.Slider { Width = 300, HorizontalAlignment = Wpf.HorizontalAlignment.Left, Margin = new Wpf.Thickness(0, 8, 0, 0), Maximum = 100, Value = 60 },
                },
            },
        };
    }
}
