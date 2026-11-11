using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Instrumental.InstantReplay.Tests
{
    /// <summary>
    /// Decodes a GIF produced by <see cref="InstantReplayCamera.SaveGif"/>. Each frame is the fully composed image that
    /// a viewer would display at that point, not just the changed area that the GIF stores for it.
    /// </summary>
    internal sealed class RecordedGif : IDisposable
    {
        private const int FrameDelayPropertyId = 0x5100;

        private readonly Image image;
        private readonly FrameDimension frameDimension;
        private readonly int[] delaysInHundredthsOfASecond;

        public RecordedGif(byte[] bytes)
        {
            Bytes = bytes;
            image = Image.FromStream(new MemoryStream(bytes));
            frameDimension = new FrameDimension(image.FrameDimensionsList[0]);
            FrameCount = image.GetFrameCount(frameDimension);

            var delayBytes = image.GetPropertyItem(FrameDelayPropertyId)!.Value!;
            delaysInHundredthsOfASecond = new int[FrameCount];
            for (var i = 0; i < FrameCount; i++)
                delaysInHundredthsOfASecond[i] = BitConverter.ToInt32(delayBytes, i * 4);
        }

        public void Dispose() => image.Dispose();

        public byte[] Bytes { get; }

        public int FrameCount { get; }

        public Size Size => image.Size;

        public int GetDelayInHundredthsOfASecond(int frameIndex) => delaysInHundredthsOfASecond[frameIndex];

        public Bitmap GetFrame(int frameIndex)
        {
            image.SelectActiveFrame(frameDimension, frameIndex);
            return new Bitmap(image);
        }

        /// <summary>
        /// Compare the result using <see cref="Color.ToArgb"/>, because <see cref="Color"/> equality also compares
        /// color names.
        /// </summary>
        public Color GetPixel(int frameIndex, int x, int y)
        {
            using var frame = GetFrame(frameIndex);
            return frame.GetPixel(x, y);
        }
    }
}
