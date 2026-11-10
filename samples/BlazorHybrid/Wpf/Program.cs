using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlazorHybridWpfSample;
using Instrumental.InstantReplay;
using InstantReplaySamples;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.DependencyInjection;

internal static class Program
{
    private static int windowCount;
    private static IServiceProvider services = null!;

    [STAThread]
    private static void Main()
    {
        InstantReplayCamera.Start(reportBackgroundException: ex => MessageBox.Show(
            ex.ToString(),
            "Unhandled exception in Instrumental.InstantReplay",
            MessageBoxButton.OK,
            MessageBoxImage.Error));

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddWpfBlazorWebView();
        services = serviceCollection.BuildServiceProvider();

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

        var blazorWebView = new BlazorWebView { HostPage = @"wwwroot\index.html", Services = services, Height = 190, Margin = new Thickness(0, 12, 0, 0) };
        blazorWebView.RootComponents.Add(new RootComponent { Selector = "#app", ComponentType = typeof(BlazorContent) });

        return new Window
        {
            Title = isMainWindow ? "Blazor Hybrid in WPF sample" : $"Blazor Hybrid in WPF sample: window {number}",
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
                        Text = isMainWindow ? "Blazor Hybrid in WPF" : $"Window {number}",
                        FontSize = 24,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x5C, 0x2D, 0x91)),
                    },
                    new TextBlock { Text = SampleGif.Instructions, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 12) },
                    new TextBox { Text = "Type here", Width = 300, HorizontalAlignment = HorizontalAlignment.Left },
                    blazorWebView,
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
