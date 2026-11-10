# Avalonia embedded in Windows Forms and WPF samples

Two apps that host Avalonia controls inside another UI framework and record themselves with Instrumental.InstantReplay.
Each window has the host's own controls plus an orange **Avalonia content** panel with an Avalonia text box, button, and
slider. Type, click around, open more windows with **Open another window**, then click **Save GIF** to save the last ten
seconds to the `Instrumental.InstantReplay` folder on your desktop and open it.

| Project | Host |
| --- | --- |
| [`InWinForms`](InWinForms/AvaloniaInWinFormsSample.csproj) | A Windows Forms app with a `WinFormsAvaloniaControlHost` from [Avalonia.Win32.Interoperability](https://www.nuget.org/packages/Avalonia.Win32.Interoperability). |
| [`InWpf`](InWpf/AvaloniaInWpfSample.csproj) | A WPF app with a `WinFormsAvaloniaControlHost` inside a `WindowsFormsHost`. Avalonia has no WPF host control; this is the approach [its maintainers recommend](https://github.com/AvaloniaUI/Avalonia/discussions/19715). |

Both use the Avalonia setup in [`EmbeddedAvalonia.cs`](EmbeddedAvalonia.cs). The `net10.0-windows` builds use Avalonia
12.1.6, and the `net48` builds use Avalonia 11.3.23, because Avalonia 12 requires .NET 8 or later.

## Avalonia needs one setting

As with a standalone Avalonia app (see the [Avalonia sample](../Readme.md)), Avalonia's default Windows options present
through Windows.UI.Composition, which InstantReplay can't capture: the GIF shows the host's controls around a blank
rectangle. The samples therefore set `CompositionMode` when they initialize Avalonia, before creating any host control:

```csharp
AppBuilder.Configure<EmbeddedAvaloniaApp>()
    .UsePlatformDetect()
    .With(new Win32PlatformOptions
    {
        CompositionMode = [Win32CompositionMode.RedirectionSurface],
    })
    .SetupWithoutStarting();
```

## Requirements

- Windows 10 or later and the .NET 10 SDK (or later).
- For `net48`: .NET Framework 4.8 (included in Windows 10 and 11).

## Running the samples

From the repository root:

```powershell
dotnet run --project samples\Avalonia\Embedded\InWinForms --framework net10.0-windows
dotnet run --project samples\Avalonia\Embedded\InWinForms --framework net48
dotnet run --project samples\Avalonia\Embedded\InWpf --framework net10.0-windows
dotnet run --project samples\Avalonia\Embedded\InWpf --framework net48
```

## What you'll see

The GIF shows the host's controls and the Avalonia panel in every window. The Avalonia panel is a child window of the
host window, and with `RedirectionSurface` Avalonia presents into the window's own surface, which is where InstantReplay
copies pixels from.
