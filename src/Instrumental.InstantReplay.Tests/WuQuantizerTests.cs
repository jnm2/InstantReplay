using System.Collections.Generic;
using System.Drawing;
using NUnit.Framework;
using Shouldly;

namespace Instrumental.InstantReplay.Tests
{
    public static class WuQuantizerTests
    {
        [Test]
        public static void An_image_with_a_single_color_is_reproduced_exactly()
        {
            using var composition = new Composition(10, 10, CompositionDrawing.BitsPerPixel);
            CompositionDrawing.Fill(composition, new Rectangle(0, 0, 10, 10), Color.FromArgb(200, 100, 50));

            var result = Quantize(composition, 10, 10);

            result.ShouldAllBe(pixel => pixel.ToArgb() == Color.FromArgb(200, 100, 50).ToArgb());
        }

        [Test]
        public static void An_image_with_a_few_distinct_colors_is_reproduced_exactly()
        {
            using var composition = new Composition(20, 10, CompositionDrawing.BitsPerPixel);
            CompositionDrawing.Fill(composition, new Rectangle(0, 0, 10, 5), Color.FromArgb(255, 0, 0));
            CompositionDrawing.Fill(composition, new Rectangle(10, 0, 10, 5), Color.FromArgb(0, 255, 0));
            CompositionDrawing.Fill(composition, new Rectangle(0, 5, 10, 5), Color.FromArgb(0, 0, 255));
            CompositionDrawing.Fill(composition, new Rectangle(10, 5, 10, 5), Color.FromArgb(255, 255, 255));

            var result = Quantize(composition, 20, 10);

            result.ConvertAll(pixel => pixel.ToArgb()).ShouldBe(CompositionDrawing.GetPixels(composition, new UInt16Rectangle(0, 0, 20, 10)).ConvertAll(pixel => pixel.ToArgb()));
        }

        [Test]
        public static void The_palette_has_at_most_256_colors_and_every_pixel_has_an_index_into_it()
        {
            const int width = 64;
            const int height = 64;
            using var composition = new Composition(width, height, CompositionDrawing.BitsPerPixel);
            for (var y = 0; y < height; y += 4)
            {
                for (var x = 0; x < width; x += 4)
                    CompositionDrawing.Fill(composition, new Rectangle(x, y, 4, 4), Color.FromArgb(x * 4, y * 4, (x + y) * 2));
            }

            var quantizer = new WuQuantizer();
            var palette = new (byte R, byte G, byte B)[256];
            var indexedImage = new byte[width * height];
            quantizer.Quantize(composition.EnumerateRange(new UInt16Rectangle(0, 0, width, height)), palette, out var paletteLength, indexedImage, out var indexedImageLength);

            paletteLength.ShouldBeInRange(1u, 256u);
            indexedImageLength.ShouldBe((uint)(width * height));
            indexedImage.ShouldAllBe(index => index < paletteLength);
        }

        private static List<Color> Quantize(Composition composition, ushort width, ushort height)
        {
            var quantizer = new WuQuantizer();
            var palette = new (byte R, byte G, byte B)[256];
            var indexedImage = new byte[width * height];

            quantizer.Quantize(composition.EnumerateRange(new UInt16Rectangle(0, 0, width, height)), palette, out _, indexedImage, out var indexedImageLength);

            var result = new List<Color>();
            for (var i = 0; i < indexedImageLength; i++)
            {
                var (r, g, b) = palette[indexedImage[i]];
                result.Add(Color.FromArgb(r, g, b));
            }

            return result;
        }

    }
}
