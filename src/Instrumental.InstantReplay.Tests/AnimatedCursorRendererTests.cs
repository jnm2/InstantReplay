using System;
using System.ComponentModel;
using System.Drawing;
using NUnit.Framework;
using Shouldly;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Instrumental.InstantReplay.Tests
{
    public static class AnimatedCursorRendererTests
    {
        private const ushort Size = 96;

        [Test]
        public static void Every_drawn_pixel_is_inside_the_changed_area()
        {
            using var composition = new Composition(Size, Size, CompositionDrawing.BitsPerPixel);

            new AnimatedCursorRenderer().Render(composition.DeviceContext, LoadSystemCursor(TestPInvoke.IDC_ARROW), 30, 20, out var changedArea);

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
            var cursor = LoadSystemCursor(TestPInvoke.IDC_WAIT);

            for (var i = 0; i < 100; i++)
            {
                renderer.Render(composition.DeviceContext, cursor, 30, 20, out var changedArea);
                changedArea.IsEmpty.ShouldBeFalse();
            }
        }

        private static HCURSOR LoadSystemCursor(PCWSTR id)
        {
            var cursor = TestPInvoke.LoadCursor(default, id);
            if (cursor.IsNull) throw new Win32Exception();
            return cursor;
        }
    }
}
