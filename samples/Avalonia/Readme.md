# Avalonia UI (Windows) sample

A small code-only Avalonia desktop app that records itself with Instrumental.InstantReplay. Type in the text box, drag
the slider, open more windows with **Open another window**, then click **Save GIF** to save the last ten seconds to
`Desktop\Instrumental.InstantReplay` and open it.

The app code is shared by every project in [`AvaloniaSample.cs`](AvaloniaSample.cs):

| Project | What it is |
| --- | --- |
| [`Avalonia11`](Avalonia11/Avalonia11Sample.csproj) | Avalonia 11.3.23, for .NET 10 and .NET Framework 4.8 (Avalonia 12 requires .NET 8 or later). |
| [`Avalonia12`](Avalonia12/Avalonia12Sample.csproj) | Avalonia 12.1.6, for .NET 10. |

To see Avalonia content embedded in a Windows Forms or WPF app, see [`Embedded`](Embedded/Readme.md).

## Avalonia needs one setting

With Avalonia's default Windows options, InstantReplay records **black** frames. The sample therefore configures its
`AppBuilder` like this, which is what you should do in your own app:

```csharp
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .With(new Win32PlatformOptions
    {
        CompositionMode = [Win32CompositionMode.RedirectionSurface],
    })
```

Why: by default, Avalonia renders with ANGLE (Direct3D 11) and presents through Windows.UI.Composition, creating its
windows with `WS_EX_NOREDIRECTIONBITMAP` ([WindowImpl.cs](https://github.com/AvaloniaUI/Avalonia/blob/12.1.6/src/Windows/Avalonia.Win32/WindowImpl.cs)).
Such a window has no GDI surface, so InstantReplay's `BitBlt` from the window DC reads black. `RedirectionSurface` keeps
GPU rendering and presents frames through the window's own surface, which InstantReplay can read even when the window is
covered by another window.

Downsides of `RedirectionSurface`:

* Avalonia calls it a "compatibility fallback for older systems. Some Avalonia features may not work"
  ([docs](https://docs.avaloniaui.net/docs/platform-specific-guides/windows/#composition-mode)); in particular,
  `AcrylicBlur` and `Mica` window backdrops need `WinUIComposition`.
* It's capped at 60 FPS, and a maintainer has said it "is legacy and won't be supported eventually, when we completely
  drop Windows 7" ([AvaloniaUI/Avalonia#21053](https://github.com/AvaloniaUI/Avalonia/issues/21053#issuecomment-4167457254)).

Alternatives: `RenderingMode = [Win32RenderingMode.Software]` also works, at the cost of GPU rendering. Don't use
`CompositionMode = [Win32CompositionMode.LowLatencyDxgiSwapChain]`: its flip-model swap chain bypasses the window's
surface, so InstantReplay records whatever is on screen at the window's location, including other apps. `Wgl` and
`Vulkan` rendering currently work only because they aren't integrated with the Windows compositor yet, which is planned
([AvaloniaUI/Avalonia#19378](https://github.com/AvaloniaUI/Avalonia/issues/19378)).

The `Win32PlatformOptions` API and defaults are the same in Avalonia 11 and 12: `RenderingMode` defaults to
`[AngleEgl, Software]` and `CompositionMode` to `[WinUIComposition, DirectComposition, RedirectionSurface]`
([11.3.23](https://github.com/AvaloniaUI/Avalonia/blob/11.3.23/src/Windows/Avalonia.Win32/Win32PlatformOptions.cs),
[12.1.6](https://github.com/AvaloniaUI/Avalonia/blob/12.1.6/src/Windows/Avalonia.Win32/Win32PlatformOptions.cs),
[docs](https://docs.avaloniaui.net/docs/platform-specific-guides/windows/#configuring-win32-platform-options)). If no
Direct3D 11 device is available (some VMs and remote sessions), Avalonia falls back to software rendering by itself and
the defaults happen to work.

## Requirements

* Windows 10 or later.
* The .NET 10 SDK or later.

The .NET 10 projects target `net10.0-windows` rather than Avalonia's usual `net10.0`, because Instrumental.InstantReplay has no
plain `net10.0` build; a `net10.0` app would fall back to its `net35` build with warning NU1701.

## Running

From the repository root:

```powershell
# The recommended configuration (CompositionMode = RedirectionSurface). Saved GIFs show the windows.
dotnet run --project samples\Avalonia\Avalonia12
dotnet run --project samples\Avalonia\Avalonia11 --framework net10.0-windows
dotnet run --project samples\Avalonia\Avalonia11 --framework net48

# Software rendering. Saved GIFs show the windows.
dotnet run --project samples\Avalonia\Avalonia12 -- --software
```

Each window shows the active mode under its heading.

## Deployment

Avalonia on Windows is an ordinary Win32 desktop process: it "uses the Win32 API directly on Windows"
([Avalonia docs: Windows](https://docs.avaloniaui.net/docs/platform-specific-guides/windows/)). Apps ship as a plain
EXE, installer or ZIP, or as an MSIX
([Packaging apps for Windows](https://docs.avaloniaui.net/tools/parcel/packaging-for-windows)). Neither restricts the
user32/gdi32 calls InstantReplay makes:

* Unpackaged: a normal medium-integrity process.
* MSIX: Avalonia apps are packaged as full-trust desktop apps (`EntryPoint="Windows.FullTrustApplication"` and
  `runFullTrust`), which "don't run in an AppContainer"
  ([App capability declarations](https://learn.microsoft.com/windows/apps/package-and-deploy/app-capability-declarations)).
  Packaging makes no difference to what InstantReplay records.
* Microsoft Store: accepts Win32 apps built with any framework, as MSIX or as an unpackaged EXE/MSI
  ([Distribute your Win32 app through Microsoft Store](https://learn.microsoft.com/windows/apps/distribute-through-store/how-to-distribute-your-win32-app-through-microsoft-store)).

## Supported Avalonia versions

As of 2026-11-09:

* **12.x** ([12.1.6](https://github.com/AvaloniaUI/Avalonia/releases/tag/12.1.6)): current stable, and the only series
  receiving security fixes ([SECURITY.md](https://github.com/AvaloniaUI/Avalonia/blob/main/SECURITY.md#versions-receiving-security-fixes)).
  Requires .NET 8 or later ([breaking changes](https://docs.avaloniaui.net/docs/avalonia12-breaking-changes)).
* **11.x** ([11.3.23](https://github.com/AvaloniaUI/Avalonia/releases/tag/11.3.23)): still gets bug-fix releases, but
  SECURITY.md lists "11.x and older: Not provided" for security fixes.
* **0.10 and older**: unsupported.

Avalonia doesn't publish an LTS calendar for the open-source framework. Commercial support covers "the major version
available at the time of purchase" ([Schedule 3: Product Support](https://avaloniaui.net/legal/3), §3.5).
