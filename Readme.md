# Instrumental.InstantReplay [![Feedz badge](https://img.shields.io/feedz/vpre/jnm2/prerelease/Instrumental.InstantReplay)](https://feedz.io/portal/org/jnm2/repository/prerelease/packages "Feedz (prereleases)") [![Build status badge](https://github.com/jnm2/InstantReplay/workflows/CI/badge.svg)](https://github.com/jnm2/InstantReplay/actions?query=workflow%3ACI "Build status")

Produces an animated GIF on demand of the last ten seconds of a Windows desktop app’s user interface. This can be useful to include in error reports or to help understand how an unusual situation came about.

> [!NOTE]
> The package, assembly, and namespace have been renamed from `Techsola.InstantReplay` to `Instrumental.InstantReplay`. When upgrading, replace the old package reference and update `using` directives. The prerelease feed has moved to Feedz; update your package source as shown in [Set up](#set-up).

### Goals

- **Low resource usage** while recording

- **Privacy**: never captures content from other apps

- **Ease of consumption**: the right thing happens if you double-click a .gif file on Windows or if you open a .gif attachment in a web browser

- **Fast generation** when a GIF is requested

### Non-goals

- Optimizing GIF **file size** (unless it also speeds up GIF creation)

- Pixel-perfect recording of **non-client** areas of the app windows (but improvements will be considered)

## Is this for me?

While other integrations could happen in the future, right now this library only works with Windows desktop applications that have access to native Win32 APIs.

Apps can target .NET Framework 3.5, .NET Framework 4.8+, or .NET 10+, as far as the app model itself supports them.

| App model | Supported |
| --- | --- |
| Windows Forms and WPF, including either one hosted in the other (`ElementHost`, `WindowsFormsHost`) | ✔ |
| Avalonia, standalone or hosted in Windows Forms or WPF | ⚠ Only with a configuration change |
| Uno Platform, Skia Desktop target (`-desktop`) | ✔ |
| Blazor Hybrid and other WebView2 content in WPF (`BlazorWebView`, `WebView2CompositionControl`) | ✔ |
| Blazor Hybrid and other WebView2 content in Windows Forms, and the `WebView2` control in WPF | ❌ |
| WinUI 3, including XAML Islands, .NET MAUI, and Uno Platform’s Windows App SDK target | ❌ |
| UWP | ❌ |

Support depends on two things: whether the app is able to [invoke](https://learn.microsoft.com/dotnet/standard/native-interop/pinvoke) native Windows functions such as [`BitBlt`](https://learn.microsoft.com/windows/win32/api/wingdi/nf-wingdi-bitblt) and [`EnumWindows`](https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-enumwindows), and whether the app model draws its windows in a way that `BitBlt` can read. WinUI 3 and the `WebView2` control draw with DirectComposition, so their content records as blank. In WPF, use `WebView2CompositionControl` instead of `WebView2`. The WPF `BlazorWebView` already does this starting in .NET 10.

By default, Avalonia’s windows also record as black. To fix this, add `.With(new Win32PlatformOptions { CompositionMode = [Win32CompositionMode.RedirectionSurface] })` to your `AppBuilder`. GPU rendering is kept.

To see InstantReplay recording each supported app model, run the [samples](samples/Readme.md).

## How to use

### Set up

Until this library is released to nuget.org, add this package source to a `nuget.config` file at the root of your project’s source repository:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="jnm2 prerelease" value="https://f.feedz.io/jnm2/prerelease/nuget/index.json" />
  </packageSources>
</configuration>
```

If your project was open in Visual Studio when you edited the `nuget.config` file, close the solution and reopen it. Then use the NuGet package manager to install the latest available version of the package `Instrumental.InstantReplay` into your app’s startup project. Make sure that the ‘Include prerelease’ box is checked and the selected package source is ‘All.’

Now that the package is added to your project, add a call to `InstantReplayCamera.Start();` before your app’s first window is shown. (The namespace to include is `Instrumental.InstantReplay`.) This call only needs to be made once in the lifetime of the process. Subsequent calls are ignored.

For a Windows Forms app, the ideal place for this call is in `Program.Main` before `Application.Run` is called.

### Profit

Whenever you want a GIF of the last ten seconds of the app’s user interface, call `InstantReplayCamera.SaveGif();` to obtain a byte array containing an animated GIF. (Or `null`, if there are currently no frames to save.) A good place to do this is in your app’s top-level unhandled exception reporter so that you get a recording of the UI along with the exception information.

ℹ Consider calling `InstantReplayCamera.SaveGif` on a non-UI thread using `Task.Run` due to the CPU-blocking work it takes to encode a GIF. This way the user interface doesn't pause for even a split second.

## Debugging into Instrumental.InstantReplay source

Stepping into Instrumental.InstantReplay source code, pausing the debugger while execution is inside Instrumental.InstantReplay code and seeing the source, and setting breakpoints in Instrumental.InstantReplay all require loading symbols for Instrumental.InstantReplay. To do this in Visual Studio:

1. Go to Debug > Options, and uncheck ‘Enable Just My Code.’ (It’s a good idea to reenable this as soon as you’re finished with the task that requires debugging into a specific external library.)  
   ℹ *Before* doing this, because Visual Studio can become unresponsive when attempting to load symbols for absolutely everything, I recommend going to Debugging > Symbols within the Options window and selecting ‘Load only specified modules.’

2. If you are using a prerelease version of Instrumental.InstantReplay package, go to Debugging > Symbols within the Options window and add this as a new symbol location: `https://f.feedz.io/jnm2/prerelease/symbols`  
   If you are using a version that was released to nuget.org, enable the built-in ‘NuGet.org Symbol Server’ symbol location.

3. If ‘Load only specified modules’ is selected in Options > Debugging > Symbols, you will have to explicitly tell Visual Studio to load symbols for Instrumental.InstantReplay. One way to do this while debugging is to go to Debug > Windows > Modules and right-click on Instrumental.InstantReplay. Select ‘Load Symbols’ if you only want to do it for the current debugging session. Select ‘Always Load Automatically’ if you want to load symbols now and also add the file name to a list so that Visual Studio loads Instrumental.InstantReplay symbols in all future debug sessions when Just My Code is disabled.
