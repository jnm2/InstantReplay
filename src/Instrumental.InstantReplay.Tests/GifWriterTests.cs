using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Shouldly;

namespace Instrumental.InstantReplay.Tests
{
    public static class GifWriterTests
    {
        [Test]
        public static void Single_pixel_image_round_trips()
        {
            var palette = new[] { ((byte)12, (byte)34, (byte)56), ((byte)0, (byte)0, (byte)0) };

            using var gif = new RecordedGif(WriteGif(1, 1, new[] { new Frame(palette, new byte[] { 0 }, delay: 0) }));

            gif.Size.ShouldBe(new System.Drawing.Size(1, 1));
            gif.GetPixel(0, 0, 0).ShouldBeColor(System.Drawing.Color.FromArgb(12, 34, 56));
        }

        [TestCase(1, 7, 5)]
        [TestCase(2, 16, 16)]
        [TestCase(4, 33, 9)]
        [TestCase(8, 300, 200)]
        public static void Noisy_images_round_trip(int bitsPerPixel, int width, int height)
        {
            var paletteLength = 1 << bitsPerPixel;
            var palette = new (byte R, byte G, byte B)[paletteLength];
            for (var i = 0; i < paletteLength; i++)
                palette[i] = ((byte)i, (byte)(255 - i), (byte)(i * 7));

            // Noise makes the LZW code table fill up, so large images also exercise its resets.
            var random = new Random(42);
            var indexedImage = new byte[width * height];
            for (var i = 0; i < indexedImage.Length; i++)
                indexedImage[i] = (byte)random.Next(paletteLength);

            using var gif = new RecordedGif(WriteGif((ushort)width, (ushort)height, new[] { new Frame(palette, indexedImage, delay: 0) }));

            using var frame = gif.GetFrame(0);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var (r, g, b) = palette[indexedImage[(y * width) + x]];
                    var actual = frame.GetPixel(x, y);
                    if (actual.R != r || actual.G != g || actual.B != b)
                        Assert.Fail($"Pixel ({x}, {y}) is ({actual.R}, {actual.G}, {actual.B}) instead of ({r}, {g}, {b}).");
                }
            }
        }

        [Test]
        public static void Repetitive_images_round_trip()
        {
            const int width = 500;
            const int height = 100;
            var palette = new[] { ((byte)255, (byte)0, (byte)0), ((byte)0, (byte)0, (byte)255) };
            var indexedImage = new byte[width * height];
            for (var i = 0; i < indexedImage.Length; i++)
                indexedImage[i] = (byte)(i / 3 % 2);

            using var gif = new RecordedGif(WriteGif(width, height, new[] { new Frame(palette, indexedImage, delay: 0) }));

            gif.GetPixel(0, 0, 0).ShouldBeColor(System.Drawing.Color.FromArgb(255, 0, 0));
            gif.GetPixel(0, 3, 0).ShouldBeColor(System.Drawing.Color.FromArgb(0, 0, 255));
            gif.GetPixel(0, 499, 99).ShouldBeColor(System.Drawing.Color.FromArgb(255, 0, 0));
        }

        [Test]
        public static void Frames_and_delays_round_trip()
        {
            var red = new[] { ((byte)255, (byte)0, (byte)0), ((byte)0, (byte)0, (byte)0) };
            var green = new[] { ((byte)0, (byte)255, (byte)0), ((byte)0, (byte)0, (byte)0) };

            using var gif = new RecordedGif(WriteGif(2, 2, new[]
            {
                new Frame(red, new byte[4], delay: 10),
                new Frame(green, new byte[4], delay: 400),
            }));

            gif.FrameCount.ShouldBe(2);
            gif.GetDelayInHundredthsOfASecond(0).ShouldBe(10);
            gif.GetDelayInHundredthsOfASecond(1).ShouldBe(400);
            gif.GetPixel(0, 1, 1).ShouldBeColor(System.Drawing.Color.FromArgb(255, 0, 0));
            gif.GetPixel(1, 1, 1).ShouldBeColor(System.Drawing.Color.FromArgb(0, 255, 0));
        }

        private static byte[] WriteGif(ushort width, ushort height, IEnumerable<Frame> frames)
        {
            using var stream = new MemoryStream();
            var writer = new GifWriter(stream);

            writer.BeginStream(
                width,
                height,
                globalColorTable: false,
                sourceImageBitsPerPrimaryColor: 8,
                globalColorTableIsSorted: false,
                globalColorTableSize: 0,
                globalColorTableBackgroundColorIndex: 0);

            writer.WriteLoopingExtensionBlock();

            foreach (var frame in frames)
            {
                var bitsPerPixel = 1;
                while (1 << bitsPerPixel < frame.Palette.Length) bitsPerPixel++;

                writer.WriteGraphicControlExtensionBlock(frame.Delay);
                writer.WriteImageDescriptor(0, 0, width, height, localColorTable: true, isInterlaced: false, localColorTableIsSorted: false, localColorTableSize: (byte)(bitsPerPixel - 1));
                writer.WriteColorTable(frame.Palette, 1 << bitsPerPixel);
                writer.WriteImageData(frame.IndexedImage, (uint)frame.IndexedImage.Length, (byte)bitsPerPixel);
            }

            writer.EndStream();
            return stream.ToArray();
        }

        private sealed class Frame
        {
            public Frame((byte R, byte G, byte B)[] palette, byte[] indexedImage, ushort delay)
            {
                Palette = palette;
                IndexedImage = indexedImage;
                Delay = delay;
            }

            public (byte R, byte G, byte B)[] Palette { get; }
            public byte[] IndexedImage { get; }
            public ushort Delay { get; }
        }
    }
}
