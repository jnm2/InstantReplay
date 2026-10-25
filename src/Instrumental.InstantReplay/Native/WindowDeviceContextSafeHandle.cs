using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Instrumental.InstantReplay.Native;

// Workaround for https://github.com/microsoft/CsWin32/issues/209
internal sealed class WindowDeviceContextSafeHandle : SafeHandle
{
    public WindowDeviceContextSafeHandle(HWND hWnd, IntPtr handle)
        : base(invalidHandleValue: IntPtr.Zero, ownsHandle: true)
    {
        HWnd = hWnd;
        SetHandle(handle);
    }

    public HWND HWnd { get; }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        return PInvoke.ReleaseDC(HWnd, (HDC)handle) == 1;
    }
}
