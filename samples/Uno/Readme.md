# Uno Platform sample

An Uno Platform app (Skia Desktop target, `net10.0-desktop`) that records itself with Instrumental.InstantReplay. Type
in the text box, drag the slider, open more windows with **Open another window**, then click **Save GIF** to save the
last ten seconds to the `Instrumental.InstantReplay` folder on your desktop and open it.

[App.xaml](App.xaml) only merges the standard control styles. [App.xaml.cs](App.xaml.cs) builds the windows in code and
calls `InstantReplayCamera.Start` in `OnLaunched` before the first window is shown.

## Requirements

- Windows 10 or 11 and the .NET 10 SDK (or later).
- No workloads, Visual Studio, or Uno.Check: the project pins the Uno.Sdk version in its `Sdk="Uno.Sdk/6.7.45"`
  attribute, and `dotnet build` restores it from nuget.org.

## Running the sample

From the repository root:

```powershell
# Uno's default renderer (OpenGL)
dotnet run --project samples\Uno

# A specific renderer
dotnet run --project samples\Uno -- --renderer opengl
dotnet run --project samples\Uno -- --renderer software
dotnet run --project samples\Uno -- --renderer vulkan
```

The small line at the bottom of the main window shows which renderer [Program.cs](Program.cs) asked for with
`UseWin32(b => b.RenderingBackend(...))`. If the requested renderer isn't available, Uno falls back without telling the
app: Vulkan to OpenGL or software, and OpenGL to software
([Rendering Backend Selection](https://platform.uno/docs/articles/features/using-skia-desktop.html#rendering-backend-selection)).

## What you'll see

The GIF shows every window, with whichever renderer you pick. Uno draws the UI itself with SkiaSharp into an ordinary
Win32 window. With OpenGL it presents with `SwapBuffers` on the window's own device context, and the software renderer
copies a bitmap into the window with `BitBlt`. Either way the pixels end up in the window's normal redirection surface,
which is where InstantReplay copies them from.

Vulkan worked on an NVIDIA GPU. Whether a Vulkan swapchain is visible to `BitBlt` depends on the GPU driver: a driver
that presents Vulkan through a DXGI flip-model swapchain (for example, NVIDIA's "Vulkan/OpenGL present method: Prefer
layered on DXGI Swapchain" setting) could make the recording black. If that happens, stay with OpenGL. Vulkan is
opt-in in Uno Platform 6.x
([Vulkan Rendering Backend](https://platform.uno/docs/articles/features/using-vulkan-rendering.html)), but Uno
Platform 7, which is in development, is planned to
[make it the default](https://github.com/unoplatform/uno/blob/master/doc/articles/migrating-to-uno-7/behavior.md#vulkan-is-the-default-rendering-backend)
on Windows.

Uno Platform's other Windows target, Windows App SDK (`netX.0-windows10.0.xxxxx`), is a WinUI 3 app underneath and
records blank windows, so there's no sample for it.

**NuGet warning:** `net10.0-desktop` isn't compatible with `net10.0-windows`, so a `PackageReference` to
Instrumental.InstantReplay falls back to the .NET Framework build with warning NU1701 (NU1702 for the
`ProjectReference` in this sample). The app works; to hide the warning, add `NoWarn="NU1701"` to the
`PackageReference`.

## Deployment

Skia Desktop apps are ordinary unpackaged Win32 processes. Uno documents folder and single-file publishing and ClickOnce
for `netX.0-desktop` ([Publishing Your App For Desktop](https://platform.uno/docs/articles/uno-publishing-desktop.html)),
none of which sandbox the app. The Microsoft Store accepts unpackaged apps through their installers
([Distribute your Win32 app through Microsoft Store](https://learn.microsoft.com/windows/apps/distribute-through-store/how-to-distribute-your-win32-app-through-microsoft-store)).

## Supported versions

This sample uses Uno Platform 6.7 (Uno.Sdk [6.7.45](https://www.nuget.org/packages/Uno.Sdk/6.7.45)), the latest
release as of 2026-11-09. Uno Platform doesn't publish a support lifecycle. In practice, only the latest minor release
gets updates ([releases](https://github.com/unoplatform/uno/releases)), so 6.x is the only supported major version:
5.x hasn't had a release since April 2025 and only supports .NET 8 and 9
([.NET version support](https://platform.uno/docs/articles/net-version-support.html)), and 7.x is still in development.
