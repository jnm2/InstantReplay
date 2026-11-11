using System.Collections.Generic;
using System.Drawing;
using Windows.Win32;

namespace Instrumental.InstantReplay.Tests
{
    internal static class CompositionDrawing
    {
        public const ushort BitsPerPixel = 24;

        public static void Fill(Composition composition, Rectangle area, Color color)
        {
            using (var graphics = Graphics.FromHdc(composition.DeviceContext.DangerousGetHandle()))
            using (var brush = new SolidBrush(color))
            {
                graphics.FillRectangle(brush, area);
            }

            PInvoke.GdiFlush();
        }

        public static void SetPixel(Composition composition, int x, int y, Color color)
        {
            Fill(composition, new Rectangle(x, y, 1, 1), color);
        }

        /// <summary>
        /// Returns the pixels in the area row by row.
        /// </summary>
        public static List<Color> GetPixels(Composition composition, UInt16Rectangle area)
        {
            PInvoke.GdiFlush();

            var pixels = new List<Color>();
            foreach (var pixel in composition.EnumerateRange(area))
                pixels.Add(Color.FromArgb(pixel.Channel1, pixel.Channel2, pixel.Channel3));
            return pixels;
        }

        public static Color GetPixel(Composition composition, int x, int y)
        {
            return GetPixels(composition, new UInt16Rectangle((ushort)x, (ushort)y, 1, 1))[0];
        }
    }
}
