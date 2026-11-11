using System.Drawing;
using NUnit.Framework;
using Shouldly;

namespace Instrumental.InstantReplay.Tests
{
    public static class CompositionTests
    {
        [Test]
        public static void New_compositions_are_black()
        {
            using var composition = new Composition(5, 3, CompositionDrawing.BitsPerPixel);

            CompositionDrawing.GetPixels(composition, new UInt16Rectangle(0, 0, 5, 3))
                .ShouldAllBe(pixel => pixel.ToArgb() == Color.Black.ToArgb());
        }

        [TestCase(1u, 4u)]
        [TestCase(4u, 12u)]
        [TestCase(5u, 16u)]
        public static void Rows_are_padded_to_a_multiple_of_four_bytes(uint width, uint expectedStride)
        {
            using var composition = new Composition(width, 2, CompositionDrawing.BitsPerPixel);

            composition.BytesPerPixel.ShouldBe((byte)3);
            composition.Stride.ShouldBe(expectedStride);
        }

        [Test]
        public static void Pixels_are_enumerated_row_by_row()
        {
            using var composition = new Composition(3, 2, CompositionDrawing.BitsPerPixel);
            CompositionDrawing.SetPixel(composition, 0, 0, Color.FromArgb(10, 20, 30));
            CompositionDrawing.SetPixel(composition, 2, 0, Color.FromArgb(40, 50, 60));
            CompositionDrawing.SetPixel(composition, 1, 1, Color.FromArgb(70, 80, 90));

            var pixels = CompositionDrawing.GetPixels(composition, new UInt16Rectangle(0, 0, 3, 2));

            pixels.ConvertAll(pixel => pixel.ToArgb()).ShouldBe(new[]
            {
                Color.FromArgb(10, 20, 30).ToArgb(), Color.Black.ToArgb(), Color.FromArgb(40, 50, 60).ToArgb(),
                Color.Black.ToArgb(), Color.FromArgb(70, 80, 90).ToArgb(), Color.Black.ToArgb(),
            });
        }

        [Test]
        public static void Enumerating_a_range_skips_pixels_outside_it()
        {
            using var composition = new Composition(4, 3, CompositionDrawing.BitsPerPixel);
            CompositionDrawing.Fill(composition, new Rectangle(1, 1, 2, 2), Color.FromArgb(255, 0, 0));

            var pixels = CompositionDrawing.GetPixels(composition, new UInt16Rectangle(1, 1, 2, 2));

            pixels.Count.ShouldBe(4);
            pixels.ShouldAllBe(pixel => pixel.ToArgb() == Color.FromArgb(255, 0, 0).ToArgb());
        }

        [Test]
        public static void Clear_blackens_only_the_given_area()
        {
            using var composition = new Composition(4, 3, CompositionDrawing.BitsPerPixel);
            CompositionDrawing.Fill(composition, new Rectangle(0, 0, 4, 3), Color.White);
            var needsGdiFlush = false;

            composition.Clear(1, 1, 2, 1, ref needsGdiFlush);

            needsGdiFlush.ShouldBeTrue();
            CompositionDrawing.GetPixel(composition, 0, 1).ToArgb().ShouldBe(Color.White.ToArgb());
            CompositionDrawing.GetPixel(composition, 1, 1).ToArgb().ShouldBe(Color.Black.ToArgb());
            CompositionDrawing.GetPixel(composition, 2, 1).ToArgb().ShouldBe(Color.Black.ToArgb());
            CompositionDrawing.GetPixel(composition, 3, 1).ToArgb().ShouldBe(Color.White.ToArgb());
            CompositionDrawing.GetPixel(composition, 1, 0).ToArgb().ShouldBe(Color.White.ToArgb());
            CompositionDrawing.GetPixel(composition, 1, 2).ToArgb().ShouldBe(Color.White.ToArgb());
        }

        [Test]
        public static void Clearing_an_empty_area_does_nothing()
        {
            using var composition = new Composition(2, 2, CompositionDrawing.BitsPerPixel);
            CompositionDrawing.Fill(composition, new Rectangle(0, 0, 2, 2), Color.White);
            var needsGdiFlush = false;

            composition.Clear(0, 0, 0, 2, ref needsGdiFlush);

            needsGdiFlush.ShouldBeFalse();
            CompositionDrawing.GetPixel(composition, 0, 0).ToArgb().ShouldBe(Color.White.ToArgb());
        }
    }
}
