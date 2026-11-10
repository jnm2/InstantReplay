# WebView2 sample

A WPF app with a WebView2 control that shows an HTML page, recording itself with Instrumental.InstantReplay. Type in
either text box, click the button in the page, open more windows with **Open another window**, then click **Save GIF**
to save the last ten seconds to the `Instrumental.InstantReplay` folder on your desktop and open it.

[Wpf/Program.cs](Wpf/Program.cs) builds the windows in code and calls `InstantReplayCamera.Start` before the first
window is shown. [Wpf/index.html](Wpf/index.html) is the page.

## Requirements

- Windows 10 version 1809 or later, or Windows 11, and the .NET 10+ SDK.
- The [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/). Windows 11 includes it.

## Running the sample

From the repository root:

```powershell
dotnet run --project samples\WebView2\Wpf --framework net10.0-windows10.0.17763.0
dotnet run --project samples\WebView2\Wpf --framework net48
```

## What you'll see

The GIF shows every window, including the blue WebView2 area as it changes.

Use `WebView2CompositionControl`, not the standard `WebView2` control. The standard control records blank in both WPF
and Windows Forms: the browser process draws into its own child window through DirectComposition, so its pixels never
reach the window that InstantReplay copies from, and the rest of the window records around an empty rectangle.
`WebView2CompositionControl` copies the browser's output with a
[`GraphicsCaptureSession`](https://learn.microsoft.com/dotnet/api/microsoft.web.webview2.wpf.webview2compositioncontrol)
and draws it as part of the WPF window, so it records. It's a drop-in replacement for `WebView2` in WPF. Windows Forms
has no equivalent; host a `WebView2CompositionControl` in an `ElementHost` instead.

On .NET, `WebView2CompositionControl` needs the Windows 10 SDK projection, so the .NET build targets
`net10.0-windows10.0.17763.0`. Targeting plain `net10.0-windows` builds, but the app crashes when the control loads
because Microsoft.Windows.SDK.NET is missing.

## Supported versions

WebView2 supports .NET Framework 4.8+ and .NET 10+
([supported programming environments](https://learn.microsoft.com/microsoft-edge/webview2/#supported-programming-environments-and-devices)).
This sample uses Microsoft.Web.WebView2 [1.0.4314.35](https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.4314.35),
the latest release as of 2026-11-09.

For Razor components in WPF, see the [Blazor Hybrid sample](../BlazorHybrid), whose `BlazorWebView` uses
`WebView2CompositionControl` already.
