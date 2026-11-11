using NUnit.Framework;
using Shouldly;

namespace Instrumental.InstantReplay.Tests
{
    public static class UInt16RectangleTests
    {
        [TestCase(0, 0, ExpectedResult = true)]
        [TestCase(0, 1, ExpectedResult = true)]
        [TestCase(1, 0, ExpectedResult = true)]
        [TestCase(1, 1, ExpectedResult = false)]
        public static bool IsEmpty_when_width_or_height_is_zero(int width, int height)
        {
            return new UInt16Rectangle(5, 5, (ushort)width, (ushort)height).IsEmpty;
        }

        [Test]
        public static void Union_is_the_bounding_rectangle()
        {
            var union = new UInt16Rectangle(10, 20, 5, 5).Union(new UInt16Rectangle(30, 0, 10, 10));

            union.ShouldBe(new UInt16Rectangle(10, 0, 30, 25));
        }

        [Test]
        public static void Union_with_an_empty_rectangle_is_the_other_rectangle()
        {
            var rectangle = new UInt16Rectangle(10, 20, 5, 5);
            var empty = new UInt16Rectangle(100, 100, 0, 7);

            rectangle.Union(empty).ShouldBe(rectangle);
            empty.Union(rectangle).ShouldBe(rectangle);
        }

        [Test]
        public static void Intersect_is_the_overlapping_area()
        {
            var intersection = new UInt16Rectangle(10, 10, 20, 20).Intersect(new UInt16Rectangle(25, 0, 20, 15));

            intersection.ShouldBe(new UInt16Rectangle(25, 10, 5, 5));
        }

        [Test]
        public static void Intersect_of_separate_rectangles_is_empty()
        {
            new UInt16Rectangle(10, 10, 5, 5).Intersect(new UInt16Rectangle(20, 20, 5, 5)).IsEmpty.ShouldBeTrue();
            new UInt16Rectangle(10, 10, 5, 5).Intersect(new UInt16Rectangle(15, 10, 5, 5)).IsEmpty.ShouldBeTrue();
        }
    }
}
