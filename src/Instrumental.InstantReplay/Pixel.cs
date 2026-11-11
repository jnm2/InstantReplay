using System.Runtime.InteropServices;

namespace Instrumental.InstantReplay
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Pixel
    {
        public byte Channel3, Channel2, Channel1;
    }
}
