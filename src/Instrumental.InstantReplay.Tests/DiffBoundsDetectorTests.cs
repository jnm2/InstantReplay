using System.Drawing;
using NUnit.Framework;
using Shouldly;

namespace Instrumental.InstantReplay.Tests
{
    public static class DiffBoundsDetectorTests
    {
        private const ushort Width = 18;
        private const ushort Height = 10;

        [Test]
        public static void Identical_images_have_no_changes()
        {
            using var first = new Composition(Width, Height, CompositionDrawing.BitsPerPixel);
            using var second = new Composition(Width, Height, CompositionDrawing.BitsPerPixel);
            CompositionDrawing.Fill(first, new Rectangle(3, 2, 5, 5), Color.Red);
            CompositionDrawing.Fill(second, new Rectangle(3, 2, 5, 5), Color.Red);
            var bounds = new UInt16Rectangle(0, 0, Width, Height);

            DiffBoundsDetector.CropToChanges(first, second, ref bounds);

            bounds.IsEmpty.ShouldBeTrue();
        }

        [TestCase(0, 0)]
        [TestCase(1, 4)]
        [TestCase(5, 3)]
        [TestCase(16, 9)]
        [TestCase(17, 9)]
        public static void Bounds_are_cropped_to_a_changed_pixel(int x, int y)
        {
            using var first = new Composition(Width, Height, CompositionDrawing.BitsPerPixel);
            using var second = new Composition(Width, Height, CompositionDrawing.BitsPerPixel);
            CompositionDrawing.SetPixel(second, x, y, Color.White);
            var bounds = new UInt16Rectangle(0, 0, Width, Height);

            DiffBoundsDetector.CropToChanges(first, second, ref bounds);

            // Changes are detected 32 bits at a time, so the bounds can include a neighboring pixel on each side.
            bounds.Top.ShouldBe((ushort)y);
            bounds.Height.ShouldBe((ushort)1);
            ((int)bounds.Left).ShouldBeInRange(x - 1, x);
            (bounds.Left + bounds.Width).ShouldBeInRange(x + 1, x + 2);
        }

        [Test]
        public static void Bounds_include_every_changed_pixel()
        {
            using var first = new Composition(Width, Height, CompositionDrawing.BitsPerPixel);
            using var second = new Composition(Width, Height, CompositionDrawing.BitsPerPixel);
            CompositionDrawing.SetPixel(second, 4, 2, Color.White);
            CompositionDrawing.SetPixel(second, 12, 7, Color.Blue);
            var bounds = new UInt16Rectangle(0, 0, Width, Height);

            DiffBoundsDetector.CropToChanges(first, second, ref bounds);

            bounds.Top.ShouldBe((ushort)2);
            bounds.Height.ShouldBe((ushort)6);
            bounds.Left.ShouldBeLessThanOrEqualTo((ushort)4);
            (bounds.Left + bounds.Width).ShouldBeGreaterThan(12);
        }

        [Test]
        public static void Changes_outside_the_bounds_are_ignored()
        {
            using var first = new Composition(Width, Height, CompositionDrawing.BitsPerPixel);
            using var second = new Composition(Width, Height, CompositionDrawing.BitsPerPixel);
            CompositionDrawing.SetPixel(second, 15, 8, Color.White);
            var bounds = new UInt16Rectangle(0, 0, 8, 5);

            DiffBoundsDetector.CropToChanges(first, second, ref bounds);

            bounds.IsEmpty.ShouldBeTrue();
        }
    }
}
