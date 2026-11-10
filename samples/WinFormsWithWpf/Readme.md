# WPF in Windows Forms sample

A Windows Forms app that hosts WPF content in an
[`ElementHost`](https://learn.microsoft.com/dotnet/api/system.windows.forms.integration.elementhost) and records itself
with Instrumental.InstantReplay. Each window has Windows Forms controls plus a green **WPF content** panel with its own
text box, button, and slider. Type, click around, open more windows with **Open another window**, then click
**Save GIF** to save the last ten seconds to the `Instrumental.InstantReplay` folder on your desktop and open it.

## Requirements

- Windows and the .NET 10 SDK (or later).
- For `net48`: .NET Framework 4.8 (included in Windows 10 and 11).
- For `net35`: .NET Framework 3.5, an optional Windows feature
  ([Install .NET Framework 3.5](https://learn.microsoft.com/dotnet/framework/install/dotnet-35-windows)).

## Running the sample

From the repository root:

```powershell
dotnet run --project samples\WinFormsWithWpf --framework net10.0-windows
dotnet run --project samples\WinFormsWithWpf --framework net48
dotnet run --project samples\WinFormsWithWpf --framework net35
```

## What you'll see

The GIF shows both the Windows Forms controls and the WPF panel on every target framework. `ElementHost` puts the WPF
content in a child window (`HwndSource`) of the form, and WPF presents it into the form's own surface, which is where
InstantReplay copies pixels from.
