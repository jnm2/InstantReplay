using System.Drawing;
using Shouldly;

namespace Instrumental.InstantReplay.Tests
{
    internal static class ColorAssertions
    {
        public static void ShouldBeColor(this Color actual, Color expected)
        {
            actual.ToArgb().ShouldBe(
                expected.ToArgb(),
                () => $"Expected #{expected.ToArgb() & 0xFFFFFF:X6} but was #{actual.ToArgb() & 0xFFFFFF:X6}");
        }
    }
}
