using System;
using System.Drawing;
using NUnit.Framework;
using Shouldly;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Instrumental.InstantReplay.Tests
{
    public static class WindowEnumeratorTests
    {
        [Test]
        public static void Only_windows_of_the_current_process_are_returned()
        {
            using var window = new TestWindow(new Rectangle(300, 300, 40, 30), Color.Red);
            var currentProcessId = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;

            var windows = new WindowEnumerator().GetCurrentWindowHandlesInZOrder();

            windows.ShouldContain((HWND)window.Handle);
            foreach (var handle in windows)
                GetProcessId(handle).ShouldBe(currentProcessId);
        }

        [Test]
        public static void Windows_are_returned_from_top_to_bottom()
        {
            using var first = new TestWindow(new Rectangle(300, 300, 40, 30), Color.Red);
            using var second = new TestWindow(new Rectangle(320, 310, 40, 30), Color.Green);
            var enumerator = new WindowEnumerator();

            first.PlaceAbove(second);
            IndexOf(enumerator, first).ShouldBeLessThan(IndexOf(enumerator, second));

            second.PlaceAbove(first);
            IndexOf(enumerator, second).ShouldBeLessThan(IndexOf(enumerator, first));
        }

        private static int IndexOf(WindowEnumerator enumerator, TestWindow window)
        {
            return Array.IndexOf(enumerator.GetCurrentWindowHandlesInZOrder(), (HWND)window.Handle);
        }

        private static uint GetProcessId(HWND window)
        {
            var processId = default(uint);
            unsafe { _ = TestPInvoke.GetWindowThreadProcessId(window, &processId); }
            return processId;
        }
    }
}
