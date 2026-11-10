using System;
using System.Drawing;
using System.Windows.Forms;
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
            MessageBoxButtons.OK,
            MessageBoxIcon.Error));

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        EmbeddedAvalonia.Initialize();
        Application.Run(CreateWindow());
    }

    private static Form CreateWindow()
    {
        var number = ++windowCount;
        var isMainWindow = number == 1;

        var status = new Label { AutoSize = true, MaximumSize = new Size(440, 0), Margin = new Padding(0, 12, 0, 0) };

        var saveButton = new Button { Text = SampleGif.ButtonText, AutoSize = true };
        saveButton.Click += async (_, _) =>
        {
            saveButton.Enabled = false;
            status.Text = "Saving…";
            status.Text = await SampleGif.SaveToDesktopAndOpenAsync();
            saveButton.Enabled = true;
        };

        var openWindowButton = new Button { Text = SampleGif.OpenWindowButtonText, AutoSize = true };
        openWindowButton.Click += (_, _) => CreateWindow().Show();

        var form = new Form
        {
            Text = isMainWindow ? "Avalonia in Windows Forms sample" : $"Avalonia in Windows Forms sample: window {number}",
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
                    Text = isMainWindow ? "Avalonia in Windows Forms" : $"Window {number}",
                    AutoSize = true,
                    Font = new Font(form.Font.FontFamily, 18, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0x5C, 0x2D, 0x91),
                },
                new Label { Text = SampleGif.Instructions, AutoSize = true, MaximumSize = new Size(440, 0), Margin = new Padding(0, 8, 0, 12) },
                new TextBox { Width = 300, Text = "Type here" },
                CreateComboBox(),
                new WinFormsAvaloniaControlHost
                {
                    Size = new Size(452, 170),
                    Margin = new Padding(0, 12, 0, 0),
                    Content = EmbeddedAvalonia.CreateContent("Hosted in a WinFormsAvaloniaControlHost"),
                },
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
}
