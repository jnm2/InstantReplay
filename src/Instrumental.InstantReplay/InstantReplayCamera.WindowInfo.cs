using System;
using Instrumental.InstantReplay.Native;
using Windows.Win32.Foundation;

namespace Instrumental.InstantReplay
{
    partial class InstantReplayCamera
    {
        private sealed class WindowState : IDisposable
        {
            private readonly CircularBuffer<Frame?> frames;
            private int disposedFrameCount;

            public long FirstSeen { get; }
            public long LastSeen { get; set; }

            public WindowState(long firstSeen, int bufferSize)
            {
                FirstSeen = firstSeen;
                LastSeen = firstSeen;
                frames = new(bufferSize);
            }

            public void Dispose()
            {
                foreach (var frame in frames.GetRawBuffer())
                    frame?.Dispose();
            }

            public void AddFrame(
                DeleteDCSafeHandle bitmapDC,
                HWND window,
                long timestamp,
                WindowMetrics windowMetrics,
                uint zOrder)
            {
                var frame = frames.GetNextRef() ??= new();
                frame.Overwrite(bitmapDC, window, timestamp, windowMetrics, zOrder);
            }

            public void AddInvisibleFrame()
            {
                frames.GetNextRef()?.SetInvisible();
            }

            public void DisposeNextFrame(out bool allFramesDisposed)
            {
                if (frames.Count == 0)
                {
                    allFramesDisposed = true;
                    return;
                }

                ref var frameRef = ref frames.GetNextRef();
                frameRef?.Dispose();
                frameRef = null;

                disposedFrameCount++;
                allFramesDisposed = disposedFrameCount >= frames.Capacity;
            }

            public Frame?[] GetFramesSnapshot() => frames.ToArray();
        }
    }
}
