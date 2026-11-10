# Blazor Hybrid sample

A WPF app (.NET 10+) with a `BlazorWebView` that hosts a Razor component, recording itself with
Instrumental.InstantReplay. Type in either text box, click the Blazor button, open more windows with **Open another
window**, then click **Save GIF** to save the last ten seconds to the `Instrumental.InstantReplay` folder on your
desktop and open it.

[Wpf/Program.cs](Wpf/Program.cs) builds the windows in code and calls `InstantReplayCamera.Start` before the first
window is shown. [Wpf/BlazorContent.razor](Wpf/BlazorContent.razor) is the Razor component, and
[Wpf/wwwroot/index.html](Wpf/wwwroot/index.html) is the page the `BlazorWebView` loads it into.

## Requirements

- Windows 10 version 1809 or later, or Windows 11, and the .NET 10+ SDK.
- The [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/). Windows 11 includes it.

## Running the sample

From the repository root:

```powershell
dotnet run --project samples\BlazorHybrid\Wpf
```

## What you'll see

The GIF shows every window, including the purple Blazor area as it changes. In .NET 10, the WPF
`BlazorWebView` hosts the page in a `WebView2CompositionControl`, which copies the browser's output with a
[`GraphicsCaptureSession`](https://learn.microsoft.com/dotnet/api/microsoft.web.webview2.wpf.webview2compositioncontrol)
and draws it as part of the WPF window. That control needs the Windows 10 SDK projection, which is why the project
targets `net10.0-windows10.0.17763.0`
([Build a WPF Blazor app](https://learn.microsoft.com/aspnet/core/blazor/hybrid/tutorials/wpf?view=aspnetcore-10.0)). The .NET 11
packages add a `UseCompositionControl` property; leave it at its default, `true`.

There's no Windows Forms sample. The Windows Forms `BlazorWebView` uses the HWND-based `WebView2` control, where the
browser process draws into its own child window through DirectComposition, so the Blazor area is blank in the GIF while
the rest of the form records. Hosting the WPF `BlazorWebView` in an `ElementHost` instead records.

The same applies to a WebView2 control used without Blazor; see the [WebView2 sample](../WebView2).

## Supported versions

Blazor Hybrid runs on .NET only, not .NET Framework. Microsoft.AspNetCore.Components.WebView.Wpf ships with .NET MAUI,
and its 10.0 versions target `net10.0-windows`
([NuGet](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.WebView.Wpf)). This sample uses 10.0.130, the
latest 10.0 release as of 2026-11-09. Blazor Hybrid supports the latest WebView2 Runtime
([supported platforms](https://learn.microsoft.com/aspnet/core/blazor/supported-platforms?view=aspnetcore-10.0)).
