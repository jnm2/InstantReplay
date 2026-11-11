using System;
using System.Drawing;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Shouldly;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Instrumental.InstantReplay.Tests
{
    public static class AnimatedCursorRendererTests
    {
        private const int IdcArrow = 32512;
        private const int IdcWait = 32514;
        private const ushort Size = 96;

        [Test]
        public static void Every_drawn_pixel_is_inside_the_changed_area()
        {
            using var composition = new Composition(Size, Size, CompositionDrawing.BitsPerPixel);

            new AnimatedCursorRenderer().Render(composition.DeviceContext, LoadSystemCursor(IdcArrow), 30, 20, out var changedArea);

            changedArea.IsEmpty.ShouldBeFalse();
            var drawnPixelCount = 0;
            var pixels = CompositionDrawing.GetPixels(composition, new UInt16Rectangle(0, 0, Size, Size));
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    if (pixels[(y * Size) + x].ToArgb() == Color.Black.ToArgb()) continue;

                    drawnPixelCount++;
                    if (x < changedArea.Left || x >= changedArea.Left + changedArea.Width || y < changedArea.Top || y >= changedArea.Top + changedArea.Height)
                        Assert.Fail($"Pixel ({x}, {y}) was drawn outside of the changed area ({changedArea}).");
                }
            }

            drawnPixelCount.ShouldBeGreaterThan(0);
        }

        [Test]
        public static void Animated_cursors_can_be_rendered_repeatedly()
        {
            using var composition = new Composition(Size, Size, CompositionDrawing.BitsPerPixel);
            var renderer = new AnimatedCursorRenderer();
            var cursor = LoadSystemCursor(IdcWait);

            for (var i = 0; i < 100; i++)
            {
                renderer.Render(composition.DeviceContext, cursor, 30, 20, out var changedArea);
                changedArea.IsEmpty.ShouldBeFalse();
            }
        }

        private static HCURSOR LoadSystemCursor(int id)
        {
            var handle = LoadCursor(IntPtr.Zero, (IntPtr)id);
            if (handle == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
            return (HCURSOR)handle;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);
    }
}
