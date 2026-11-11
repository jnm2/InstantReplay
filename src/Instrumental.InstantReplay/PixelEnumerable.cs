namespace Instrumental.InstantReplay
{
    internal readonly ref struct PixelEnumerable
    {
        private readonly unsafe byte* start;
        private readonly uint width;
        private readonly uint stride;
        private readonly uint height;

        public unsafe PixelEnumerable(byte* start, uint width, uint stride, uint height)
        {
            this.start = start;
            this.width = width;
            this.stride = stride;
            this.height = height;
        }

        public PixelEnumerator GetEnumerator()
        {
            unsafe
            {
                return new(start, width, stride, height);
            }
        }
    }
}
