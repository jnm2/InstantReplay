using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Instrumental.InstantReplay.Native;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Instrumental.InstantReplay
{
    public static partial class InstantReplayCamera
    {
        private sealed class Frame : IDisposable
        {
            public const int BitsPerPixel = 24;

            private DeleteObjectSafeHandle? bitmap;
            private int bitmapWidth;
            private int bitmapHeight;

            public WindowMetrics WindowMetrics { get; private set; }
            public uint ZOrder { get; private set; }

            /// <summary>
            /// The timestamp of the timer tick for which this frame was successfully captured, or <see langword="null"/>
            /// if the most recent capture attempt did not succeed. Used to align frames from different windows during
            /// composition so that a missed or failed capture in one window can never shift that window's frames
            /// relative to other windows' frames.
            /// </summary>
            public long? Timestamp { get; private set; }

            public void Dispose()
            {
                bitmap?.Dispose();
            }

            public void Overwrite(
                DeleteDCSafeHandle bitmapDC,
                HWND window,
                long timestamp,
                WindowMetrics windowMetrics,
                uint zOrder)
            {
                // This frame object is being reused, so make sure its previous contents can't be matched up with the
                // new timestamp if anything below fails.
                Timestamp = null;

                if (windowMetrics.ClientWidth > 0 && windowMetrics.ClientHeight > 0)
                {
                    if (bitmap is null || bitmapWidth < windowMetrics.ClientWidth || bitmapHeight < windowMetrics.ClientHeight)
                    {
                        if (bitmap is null)
                        {
                            // Most of the time, windows don't resize, so save some space by not rounding up.
                            bitmapWidth = windowMetrics.ClientWidth;
                            bitmapHeight = windowMetrics.ClientHeight;
                        }
                        else
                        {
                            // Round up to the nearest 256 pixels to minimize the number of times that bitmaps are
                            // reallocated.
                            bitmapWidth = ((Math.Max(bitmapWidth, windowMetrics.ClientWidth) + 255) / 256) * 256;
                            bitmapHeight = ((Math.Max(bitmapHeight, windowMetrics.ClientHeight) + 255) / 256) * 256;

                            bitmap.Dispose();
                        }

                        var bitmapDCNeedsRelease = false;
                        bitmapDC.DangerousAddRef(ref bitmapDCNeedsRelease);
                        try
                        {
                            unsafe
                            {
                                var bitmapInfo = new BITMAPINFO
                                {
                                    bmiHeader =
                                    {
                                        biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER)),
                                        biWidth = bitmapWidth,
                                        biHeight = -bitmapHeight,
                                        biPlanes = 1,
                                        biBitCount = BitsPerPixel,
                                    },
                                };

                                bitmap = PInvoke.CreateDIBSection((HDC)bitmapDC.DangerousGetHandle(), &bitmapInfo, DIB_USAGE.DIB_RGB_COLORS, ppvBits: out _, hSection: null, offset: 0).ThrowLastErrorIfInvalid();
                            }
                        }
                        finally
                        {
                            if (bitmapDCNeedsRelease) bitmapDC.DangerousRelease();
                        }
                    }

                    // Workaround for https://github.com/microsoft/CsWin32/issues/199
                    if (PInvoke.SelectObject((HDC)bitmapDC.DangerousGetHandle(), (HGDIOBJ)bitmap.DangerousGetHandle()).IsNull)
                        throw new Win32Exception("SelectObject failed.");

                    // The window DC is acquired and released within this call rather than being cached across timer
                    // ticks, because DCs have thread affinity and timer callbacks run on arbitrary thread pool
                    // threads. ReleaseDC must be called on the same thread that called GetDC, and a DC obtained on a
                    // thread pool thread stops working if that thread exits.
                    // (https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-releasedc#remarks)
                    // A SafeHandle is deliberately not used, since its finalizer could call ReleaseDC on another thread.
                    var windowDC = PInvoke.GetDC(window);
                    if (windowDC.IsNull)
                    {
                        // This happens when the window goes away. Let this be detected on the next cycle, if it was
                        // actually due to the window closing and not some other failure. Just make sure a stale frame
                        // isn't drawn during this cycle.
                        SetInvisible();
                        return;
                    }

                    try
                    {
                        PInvoke.SetLastError(0); // Make sure a stale error code isn't reported if BitBlt fails without setting one
                        if (!PInvoke.BitBlt((HDC)bitmapDC.DangerousGetHandle(), 0, 0, windowMetrics.ClientWidth, windowMetrics.ClientHeight, windowDC, 0, 0, ROP_CODE.SRCCOPY))
                        {
                            var lastError = Marshal.GetLastWin32Error();

                            // If the window is destroyed after GetDC, BitBlt fails with no error code. Windows are
                            // hidden before they are destroyed, so a window that is no longer visible explains this.
                            if ((ERROR)lastError is ERROR.INVALID_WINDOW_HANDLE or ERROR.DC_NOT_FOUND || !PInvoke.IsWindowVisible(window))
                            {
                                // Same as above: the window is going away.
                                SetInvisible();
                                return;
                            }

                            throw lastError != 0 ? new Win32Exception(lastError) : new Win32Exception("BitBlt failed.");
                        }

                        // Call GdiFlush to find out whether the copy succeeded, so that this frame is skipped if it
                        // didn't. Both batched and non-batched calls return TRUE, so GdiFlush must always be called
                        // just in case. This also makes sure the copy is complete before the window DC is released.
                        // (https://learn.microsoft.com/windows/win32/api/wingdi/nf-wingdi-gdisetbatchlimit#remarks)
                        if (!PInvoke.GdiFlush())
                        {
                            if (PInvoke.IsWindowVisible(window))
                                throw new Win32Exception("GdiFlush failed.");

                            // Same as above: the window is going away.
                            SetInvisible();
                            return;
                        }
                    }
                    finally
                    {
                        PInvoke.ReleaseDC(window, windowDC);
                    }
                }

                WindowMetrics = windowMetrics;
                ZOrder = zOrder;
                Timestamp = timestamp;
            }

            public void SetInvisible()
            {
                WindowMetrics = default;
            }

            public void Compose(
                DeleteDCSafeHandle bitmapDC,
                DeleteDCSafeHandle compositionDC,
                (int X, int Y) compositionOffset,
                ref bool needsGdiFlush,
                out UInt16Rectangle changedArea)
            {
                if (bitmap is null || WindowMetrics.ClientWidth == 0 || WindowMetrics.ClientHeight == 0)
                {
                    changedArea = default;
                    return;
                }

                // Workaround for https://github.com/microsoft/CsWin32/issues/199
                if (PInvoke.SelectObject((HDC)bitmapDC.DangerousGetHandle(), (HGDIOBJ)bitmap.DangerousGetHandle()).IsNull)
                    throw new Win32Exception("SelectObject failed.");

                changedArea = new(
                    (ushort)(WindowMetrics.ClientLeft + compositionOffset.X),
                    (ushort)(WindowMetrics.ClientTop + compositionOffset.Y),
                    (ushort)WindowMetrics.ClientWidth,
                    (ushort)WindowMetrics.ClientHeight);

                PInvoke.SetLastError(0); // Make sure a stale error code isn't reported if BitBlt fails without setting one
                if (!PInvoke.BitBlt(
                    (HDC)compositionDC.DangerousGetHandle(),
                    changedArea.Left,
                    changedArea.Top,
                    changedArea.Width,
                    changedArea.Height,
                    (HDC)bitmapDC.DangerousGetHandle(),
                    0,
                    0,
                    ROP_CODE.SRCCOPY))
                {
                    var lastError = Marshal.GetLastWin32Error();
                    throw lastError != 0 ? new Win32Exception(lastError) : new Win32Exception("BitBlt failed.");
                }

                // Both batched and non-batched calls return TRUE, so GdiFlush must always be called just in case.
                // (https://learn.microsoft.com/windows/win32/api/wingdi/nf-wingdi-gdisetbatchlimit#remarks)
                needsGdiFlush = true;
            }
        }
    }
}
