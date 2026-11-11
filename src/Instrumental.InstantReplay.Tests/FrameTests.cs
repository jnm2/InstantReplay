using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using Instrumental.InstantReplay.Native;
using NUnit.Framework;
using Shouldly;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Instrumental.InstantReplay.Tests
{
    [HiddenDesktop]
    public static class FrameTests
    {
        [Test]
        public static void Capture_is_skipped_when_there_is_no_display_surface()
        {
            using var window = new TestWindow(new Rectangle(300, 300, 100, 100), Color.Red);
            var windowHandle = (HWND)window.Handle;
            var clientBounds = window.ClientBounds;

            // A thread on a desktop in a non-interactive window station has no display surface to read from. From that
            // thread, the window still counts as visible and GetDC succeeds, but BitBlt fails with ERROR_INVALID_HANDLE,
            // which is the error reported in issue #20.
            using var windowStation = TestPInvoke.CreateWindowStation((string?)null, 0, (uint)GENERIC_ACCESS_RIGHTS.GENERIC_ALL, null);
            if (windowStation.IsInvalid) throw new Win32Exception();

            // Must stay open until the capturing thread, which is attached to it, has exited.
            using var desktop = CreateDesktop(windowStation);

            var isWindowVisibleFromCapturingThread = false;
            var exception = (Exception?)null;
            var timestamp = (long?)null;

            var capturingThread = new Thread(() =>
            {
                try
                {
                    if (!TestPInvoke.SetThreadDesktop(desktop)) throw new Win32Exception();

                    isWindowVisibleFromCapturingThread = PInvoke.IsWindowVisible(windowHandle);

                    using var bitmapDC = new DeleteDCSafeHandle(PInvoke.CreateCompatibleDC(default));
                    if (bitmapDC.IsInvalid) throw new InvalidOperationException("CreateCompatibleDC failed.");

                    using var frame = new Camera.Frame();
                    var windowMetrics = new Camera.WindowMetrics(clientBounds.X, clientBounds.Y, clientBounds.Width, clientBounds.Height);

                    frame.Overwrite(bitmapDC, windowHandle, timestamp: 1, windowMetrics, zOrder: 0);

                    timestamp = frame.Timestamp;
                }
#pragma warning disable CA1031 // An exception on this thread would crash the test run, so it is asserted on the test thread instead.
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    exception = ex;
                }
            });

            capturingThread.Start();
            capturingThread.Join();

            exception.ShouldBeNull();
            isWindowVisibleFromCapturingThread.ShouldBe(true);
            timestamp.ShouldBeNull();
        }

        private static CloseDesktopSafeHandle CreateDesktop(SafeHandle windowStation)
        {
            // Desktops can only be created in the process's window station.
            var originalWindowStation = TestPInvoke.GetProcessWindowStation();
            if (originalWindowStation.IsNull) throw new Win32Exception();
            if (!TestPInvoke.SetProcessWindowStation(windowStation)) throw new Win32Exception();

            var desktop = TestPInvoke.CreateDesktop("Default", 0, (uint)GENERIC_ACCESS_RIGHTS.GENERIC_ALL, null);
            var createDesktopError = Marshal.GetLastWin32Error();

            // The process must be back in its original window station before anything is thrown.
            if (!TestPInvoke.SetProcessWindowStation(originalWindowStation))
            {
                var error = Marshal.GetLastWin32Error();
                desktop.Dispose();
                throw new Win32Exception(error);
            }

            if (desktop.IsInvalid) throw new Win32Exception(createDesktopError);

            return desktop;
        }
    }
}