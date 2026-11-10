# Samples

Small desktop apps that show Instrumental.InstantReplay recording each supported app model listed under
[“Is this for me?”](../Readme.md#is-this-for-me). Each sample starts recording when it launches. Type, click around, and
open more windows, then click **Save GIF**. The GIF of the last ten seconds is saved to an `Instrumental.InstantReplay`
folder on your desktop and opened.

| App model | Target frameworks | Sample |
| --- | --- | --- |
| Windows Forms | .NET Framework 3.5, .NET Framework 4.8, .NET 10 | [WinForms](WinForms) |
| WPF | .NET Framework 3.5, .NET Framework 4.8, .NET 10 | [Wpf](Wpf) |
| WPF content in Windows Forms (`ElementHost`) | .NET Framework 3.5, .NET Framework 4.8, .NET 10 | [WinFormsWithWpf](WinFormsWithWpf) |
| Windows Forms controls in WPF (`WindowsFormsHost`) | .NET Framework 3.5, .NET Framework 4.8, .NET 10 | [WpfWithWinForms](WpfWithWinForms) |
| Avalonia, with the configuration change the README describes | Avalonia 11: .NET Framework 4.8, .NET 10. Avalonia 12: .NET 10. | [Avalonia](Avalonia) |
| Avalonia in Windows Forms or WPF (`WinFormsAvaloniaControlHost`), with the same configuration change | Avalonia 11: .NET Framework 4.8. Avalonia 12: .NET 10. | [Avalonia\Embedded](Avalonia/Embedded) |
| Uno Platform, Skia Desktop target | Uno Platform 6: .NET 10 | [Uno](Uno) |
| Blazor Hybrid in WPF (`BlazorWebView`) | .NET 10 | [BlazorHybrid](BlazorHybrid) |
| WebView2 in WPF (`WebView2CompositionControl`) | .NET Framework 4.8, .NET 10 | [WebView2](WebView2) |

## Running a sample

The samples reference [Instrumental.InstantReplay](../src/Instrumental.InstantReplay) as a project, so they always run
the current source. For example:

```powershell
dotnet run --project samples\WinForms --framework net10.0-windows
dotnet run --project samples\Wpf --framework net35
dotnet run --project samples\Avalonia\Avalonia12
dotnet run --project samples\Uno
dotnet run --project samples\BlazorHybrid\Wpf
```

Each folder’s readme has more details. To open all the samples at once, open [Samples.slnx](Samples.slnx).

The samples aren’t in the main solution, so building the library doesn’t require their SDK downloads.
