using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Instrumental.InstantReplay.Native;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Instrumental.InstantReplay
{
    internal sealed partial class Camera : IDisposable
    {
        private const int MillisecondsBeforeBitBltingNewWindow = 300;
        private const int FramesPerSecond = 10;
        private const int DurationInSeconds = 10;
        private const int BufferSize = DurationInSeconds * FramesPerSecond;

        private readonly Action<Exception> reportBackgroundException;
        private readonly TimeProvider timeProvider;
        private readonly FrequencyLimiter backgroundExceptionReportLimiter;
        private readonly object frameLock = new();
        private readonly Dictionary<HWND, WindowState> infoByWindowHandle = new();
        private readonly CircularBuffer<(long Timestamp, (int X, int Y, HCURSOR Handle)? Cursor)> frames = new(BufferSize);
        private readonly SharedResultMutex<byte[]?> saveGifSharedResultMutex;

        private ITimer? timer;
        private WindowEnumerator? windowEnumerator;
        private DeleteDCSafeHandle? bitmapDC;
        private bool isDisabled;

        public Camera(Action<Exception> reportBackgroundException, TimeProvider timeProvider)
        {
            this.reportBackgroundException = reportBackgroundException ?? throw new ArgumentNullException(nameof(reportBackgroundException));
            this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

            backgroundExceptionReportLimiter = new(
                new(maximumCount: 3, withinDuration: TimeSpan.FromHours(1)),
                timeProvider.TimestampFrequency);

            saveGifSharedResultMutex = new(SaveGifCore);
        }

        /// <summary>
        /// Starts the timer that adds frames, with its first tick due immediately.
        /// </summary>
        public void Start()
        {
            // Consider varying timer frequency when there are no visible windows to e.g. 1 second
            timer = timeProvider.CreateTimer(AddFrames, state: null, dueTime: TimeSpan.Zero, period: TimeSpan.FromSeconds(1.0 / FramesPerSecond));
        }

        /// <summary>
        /// Stops adding frames and releases all buffered frames. <see cref="SaveGif"/> returns <see langword="null"/>
        /// afterwards.
        /// </summary>
        public void Dispose()
        {
            timer?.Dispose();

            lock (frameLock)
            {
                isDisabled = true;

                lock (infoByWindowHandle)
                {
                    foreach (var windowState in infoByWindowHandle.Values)
                        windowState.Dispose();

                    infoByWindowHandle.Clear();
                }

                bitmapDC?.Dispose();
                bitmapDC = null;
            }
        }

        private void AddFrames(object? state)
        {
            if (isDisabled) return;

            var now = timeProvider.GetTimestamp();

            try
            {
                var lockTaken = false;
                try
                {
#if NET35
                    lockTaken = Monitor.TryEnter(frameLock);
#else
                    Monitor.TryEnter(frameLock, ref lockTaken);
#endif
                    if (!lockTaken) return;

                    if (isDisabled) return;

                    var cursorInfo = new CURSORINFO { cbSize = (uint)Marshal.SizeOf(typeof(CURSORINFO)) };
                    if (!PInvoke.GetCursorInfo(ref cursorInfo))
                    {
                        var lastError = Marshal.GetLastWin32Error();
                        // Access is denied while the workstation is locked.
                        if ((ERROR)lastError != ERROR.ACCESS_DENIED)
                            throw new Win32Exception(lastError);
                    }

                    var currentWindows = (windowEnumerator ??= new()).GetCurrentWindowHandlesInZOrder();

                    bitmapDC ??= new DeleteDCSafeHandle(PInvoke.CreateCompatibleDC(default)).ThrowWithoutLastErrorAvailableIfInvalid(nameof(PInvoke.CreateCompatibleDC));

                    lock (infoByWindowHandle)
                    {
                        frames.Add((
                            Timestamp: now,
                            Cursor: (cursorInfo.flags & (CURSORINFO_FLAGS.CURSOR_SHOWING | CURSORINFO_FLAGS.CURSOR_SUPPRESSED)) == CURSORINFO_FLAGS.CURSOR_SHOWING
                                ? (cursorInfo.ptScreenPos.X, cursorInfo.ptScreenPos.Y, cursorInfo.hCursor)
                                : null));

                        var zOrder = 0u;

                        foreach (var window in currentWindows)
                        {
                            if (!infoByWindowHandle.TryGetValue(window, out var windowState))
                            {
                                // The window hasn't been seen before
                                if (PInvoke.IsWindowVisible(window))
                                {
                                    windowState = new(firstSeen: now, BufferSize);
                                    infoByWindowHandle.Add(window, windowState);
                                }
                            }
                            else
                            {
                                // The window has been seen before
                                if ((now - windowState.FirstSeen) < timeProvider.TimestampFrequency * MillisecondsBeforeBitBltingNewWindow / 1000)
                                {
                                    // No frames have been added yet
                                }
                                else if (!PInvoke.IsWindowVisible(window))
                                {
                                    windowState.AddInvisibleFrame();
                                }
                                else if (GetWindowMetricsIfExists(window) is { } metrics)
                                {
                                    windowState.AddFrame(bitmapDC, window, now, metrics, zOrder);
                                    zOrder++;
                                }
                                else
                                {
                                    // The window will be detected as closed
                                    continue;
                                }

                                windowState.LastSeen = now; // Keeps the window from being detected as closed
                            }
                        }

                        var closedWindowsWithNoFrames = new List<HWND>();

                        foreach (var entry in infoByWindowHandle)
                        {
                            if (entry.Value.LastSeen != now)
                            {
                                entry.Value.DisposeNextFrame(out var allFramesDisposed);

                                if (allFramesDisposed)
                                {
                                    entry.Value.Dispose();
                                    closedWindowsWithNoFrames.Add(entry.Key);
                                }
                            }
                        }

                        foreach (var window in closedWindowsWithNoFrames)
                            infoByWindowHandle.Remove(window);
                    }
                }
                finally
                {
                    if (lockTaken) Monitor.Exit(frameLock);
                }
            }
#pragma warning disable CA1031 // If this is not caught, the runtime forcibly terminates the app.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                if (backgroundExceptionReportLimiter.TryAddOccurrence(now))
                {
                    reportBackgroundException.Invoke(ex);
                }
                else
                {
                    isDisabled = true;
                    timer!.Dispose();
                }
            }
        }

        public byte[]? SaveGif() => saveGifSharedResultMutex.GetResult();

        private byte[]? SaveGifCore()
        {
            if (isDisabled) return null;

            lock (frameLock)
            {
                var frames = this.frames.ToArray();
                var framesByWindow = infoByWindowHandle.Values.Select(i => i.GetFramesSnapshot()).ToList();

                var renderer = new CompositionRenderer(frames, framesByWindow);
                if (renderer.FrameCount == 0)
                    return null;

                if (bitmapDC is not { IsInvalid: false })
                    throw new InvalidOperationException("infoByWindowHandle should be empty if bitmapDC is not valid.");

                using var composition1 = new Composition(renderer.CompositionWidth, renderer.CompositionHeight, Frame.BitsPerPixel);
                using var composition2 = new Composition(renderer.CompositionWidth, renderer.CompositionHeight, Frame.BitsPerPixel);

                var frameSink = new FrameSink(renderer.CompositionWidth, renderer.CompositionHeight);

                var comparisonBuffer = composition1;
                var emitBuffer = composition2;

                var startingTimestamp = frames[frames.Length - renderer.FrameCount].Timestamp;
                var totalEmittedDelays = 0L;

                // First frame
                var needsGdiFlush = false;
                renderer.Compose(frameIndex: 0, emitBuffer, bitmapDC, ref needsGdiFlush, out var emitBufferNonEmptyArea);
                var emitBoundingRectangle = new UInt16Rectangle(0, 0, renderer.CompositionWidth, renderer.CompositionHeight);

                var comparisonBufferNonEmptyArea = default(UInt16Rectangle);

                for (var i = 1; i < renderer.FrameCount; i++)
                {
                    comparisonBuffer.Clear(
                        comparisonBufferNonEmptyArea.Left,
                        comparisonBufferNonEmptyArea.Top,
                        comparisonBufferNonEmptyArea.Width,
                        comparisonBufferNonEmptyArea.Height,
                        ref needsGdiFlush);

                    renderer.Compose(i, comparisonBuffer, bitmapDC, ref needsGdiFlush, out comparisonBufferNonEmptyArea);

                    var boundingRectangle = emitBufferNonEmptyArea.Union(comparisonBufferNonEmptyArea);

                    // Required before accessing pixel data
                    if (needsGdiFlush)
                    {
                        if (!PInvoke.GdiFlush()) throw new Win32Exception("GdiFlush failed.");
                        needsGdiFlush = false;
                    }

                    DiffBoundsDetector.CropToChanges(emitBuffer, comparisonBuffer, ref boundingRectangle);

                    if (boundingRectangle.IsEmpty) continue;

                    var changeTimestamp = frames[i - renderer.FrameCount + frames.Length].Timestamp;
                    var timestampTicksPerHundredthOfASecond = timeProvider.TimestampFrequency / 100;
                    var totalHundredthsOfASecond = (changeTimestamp - startingTimestamp) / timestampTicksPerHundredthOfASecond;

                    frameSink.EmitFrame(emitBuffer, emitBoundingRectangle, (ushort)(totalHundredthsOfASecond - totalEmittedDelays));
                    totalEmittedDelays = totalHundredthsOfASecond;

                    var nextBuffer = emitBuffer;
                    emitBuffer = comparisonBuffer;
                    comparisonBuffer = nextBuffer;

                    var nextBufferNonEmptyArea = emitBufferNonEmptyArea;
                    emitBufferNonEmptyArea = comparisonBufferNonEmptyArea;
                    comparisonBufferNonEmptyArea = nextBufferNonEmptyArea;

                    emitBoundingRectangle = boundingRectangle;
                }

                frameSink.EmitFrame(emitBuffer, emitBoundingRectangle, delayInHundredthsOfASecond: 400);

                return frameSink.End();
            }
        }

        private static WindowMetrics? GetWindowMetricsIfExists(HWND window)
        {
            var clientTopLeft = default(Point);
            if (!PInvoke.ClientToScreen(window, ref clientTopLeft))
                return null; // This is what happens when the window handle becomes invalid.

            if (!PInvoke.GetClientRect(window, out var clientRect))
            {
                var lastError = Marshal.GetLastWin32Error();
                if ((ERROR)lastError == ERROR.INVALID_WINDOW_HANDLE) return null;
                throw new Win32Exception(lastError);
            }

            return new(clientTopLeft.X, clientTopLeft.Y, clientRect.right, clientRect.bottom);
        }

        private static byte GetBitsPerPixel(uint paletteLength)
        {
            if (paletteLength > 256)
                throw new ArgumentOutOfRangeException(nameof(paletteLength), paletteLength, "Palette length must be no greater than 256.");

#if NETFRAMEWORK
            // Distribution is expected to be heavily weighted towards large palettes
            if (paletteLength > 128) return 8;
            if (paletteLength > 64) return 7;
            if (paletteLength > 32) return 6;
            if (paletteLength > 16) return 5;
            if (paletteLength > 8) return 4;
            if (paletteLength > 4) return 3;
            if (paletteLength > 2) return 2;
            return 1;
#else
            return paletteLength <= 2 ? (byte)1 :
                (byte)((sizeof(uint) * 8) - System.Numerics.BitOperations.LeadingZeroCount(paletteLength - 1));
#endif
        }
    }
}
