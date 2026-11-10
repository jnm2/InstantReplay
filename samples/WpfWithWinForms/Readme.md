# Windows Forms in WPF sample

A WPF app that hosts Windows Forms controls in a
[`WindowsFormsHost`](https://learn.microsoft.com/dotnet/api/system.windows.forms.integration.windowsformshost) and
records itself with Instrumental.InstantReplay. Each window has WPF controls plus a green **Windows Forms content** panel
with its own text box, button, and combo box. Type, click around, open more windows with **Open another window**, then
click **Save GIF** to save the last ten seconds to the `Instrumental.InstantReplay` folder on your desktop and open it.

## Requirements

- Windows and the .NET 10 SDK (or later).
- For `net48`: .NET Framework 4.8 (included in Windows 10 and 11).
- For `net35`: .NET Framework 3.5, an optional Windows feature
  ([Install .NET Framework 3.5](https://learn.microsoft.com/dotnet/framework/install/dotnet-35-windows)).

## Running the sample

From the repository root:

```powershell
dotnet run --project samples\WpfWithWinForms --framework net10.0-windows
dotnet run --project samples\WpfWithWinForms --framework net48
dotnet run --project samples\WpfWithWinForms --framework net35
```

## What you'll see

The GIF shows both the WPF controls and the Windows Forms panel on every target framework. Windows Forms controls are
ordinary GDI child windows of the WPF window, so they draw into the window's own surface, which is where InstantReplay
copies pixels from.
