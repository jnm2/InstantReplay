using System.Collections.Generic;
using Instrumental.InstantReplay.Native;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Instrumental.InstantReplay
{
    partial class InstantReplayCamera
    {
        private readonly struct CompositionRenderer
        {
            private readonly (long Timestamp, (int X, int Y, HCURSOR Handle)? Cursor)[] frames;
            private readonly List<Frame?[]> framesByWindow;
            private readonly (int X, int Y) compositionOffset;
            private readonly AnimatedCursorRenderer cursorRenderer;

            public uint FrameCount { get; }
            public ushort CompositionWidth { get; }
            public ushort CompositionHeight { get; }

            public CompositionRenderer((long Timestamp, (int X, int Y, HCURSOR Handle)? Cursor)[] frames, List<Frame?[]> framesByWindow)
            {
                this.frames = frames;
                this.framesByWindow = AlignByTimestamp(frames, framesByWindow);

                var minLeft = int.MaxValue;
                var maxRight = int.MinValue;
                var minTop = int.MaxValue;
                var maxBottom = int.MinValue;
                var maxFrameCount = 0u;

                foreach (var frameList in this.framesByWindow)
                {
                    for (var i = 0u; i < frameList.Length; i++)
                    {
                        if (frameList[i]?.WindowMetrics is not { ClientWidth: > 0, ClientHeight: > 0 } metrics) continue;

                        var frameCount = (uint)frameList.Length - i;
                        if (maxFrameCount < frameCount) maxFrameCount = frameCount;

                        if (minLeft > metrics.ClientLeft) minLeft = metrics.ClientLeft;
                        if (minTop > metrics.ClientTop) minTop = metrics.ClientTop;
                        if (maxRight < metrics.ClientLeft + metrics.ClientWidth) maxRight = metrics.ClientLeft + metrics.ClientWidth;
                        if (maxBottom < metrics.ClientTop + metrics.ClientHeight) maxBottom = metrics.ClientTop + metrics.ClientHeight;
                    }
                }

                FrameCount = maxFrameCount;

                if (maxFrameCount == 0)
                {
                    compositionOffset = default;
                    CompositionWidth = default;
                    CompositionHeight = default;
                }
                else
                {
                    compositionOffset = (X: -minLeft, Y: -minTop);
                    CompositionWidth = checked((ushort)(maxRight - minLeft));
                    CompositionHeight = checked((ushort)(maxBottom - minTop));
                }

                cursorRenderer = new();
            }

            /// <summary>
            /// Returns frame lists that are each the same length as <paramref name="frames"/>, where each window frame
            /// is placed at the same index as the timer tick during which it was captured. Window frames that do not
            /// correspond to any of the given timer ticks, such as frames whose capture failed, are left out. This
            /// keeps windows in sync with each other and with the cursor even if a window misses adding a frame
            /// during some timer tick.
            /// </summary>
            private static List<Frame?[]> AlignByTimestamp(
                (long Timestamp, (int X, int Y, HCURSOR Handle)? Cursor)[] frames,
                List<Frame?[]> framesByWindow)
            {
                var indexByTimestamp = new Dictionary<long, int>(frames.Length);
                for (var i = 0; i < frames.Length; i++)
                    indexByTimestamp[frames[i].Timestamp] = i;

                var aligned = new List<Frame?[]>(framesByWindow.Count);

                foreach (var frameList in framesByWindow)
                {
                    var alignedFrameList = new Frame?[frames.Length];

                    foreach (var frame in frameList)
                    {
                        if (frame?.Timestamp is { } timestamp && indexByTimestamp.TryGetValue(timestamp, out var index))
                            alignedFrameList[index] = frame;
                    }

                    aligned.Add(alignedFrameList);
                }

                return aligned;
            }

            public void Compose(
                int frameIndex,
                Composition buffer,
                DeleteDCSafeHandle bitmapDC,
                ref bool needsGdiFlush,
                out UInt16Rectangle changedArea)
            {
                var windowFramesToDraw = new List<Frame>();

                foreach (var frameList in framesByWindow)
                {
                    var index = frameIndex - FrameCount + frameList.Length;
                    if (index >= 0 && frameList[index] is { WindowMetrics: { ClientWidth: > 0, ClientHeight: > 0 } } windowFrame)
                        windowFramesToDraw.Add(windowFrame);
                }

                windowFramesToDraw.Sort((a, b) => b.ZOrder.CompareTo(a.ZOrder));

                changedArea = default;

                foreach (var windowFrame in windowFramesToDraw)
                {
                    windowFrame.Compose(bitmapDC, buffer.DeviceContext, compositionOffset, ref needsGdiFlush, out var additionalChangedArea);
                    changedArea = changedArea.Union(additionalChangedArea);
                }

                var frame = frames[frameIndex - FrameCount + frames.Length];

                if (frame.Cursor is { } cursor)
                {
                    cursorRenderer.Render(buffer.DeviceContext, cursor.Handle, cursor.X + compositionOffset.X, cursor.Y + compositionOffset.Y, out var additionalChangedArea);
                    changedArea = changedArea.Union(additionalChangedArea);
                }

                changedArea = changedArea.Intersect(new(0, 0, CompositionWidth, CompositionHeight));
            }
        }
    }
}
