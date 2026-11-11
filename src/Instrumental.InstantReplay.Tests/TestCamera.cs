using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using NUnit.Framework;

namespace Instrumental.InstantReplay.Tests
{
    /// <summary>
    /// A camera whose timer ticks only when <see cref="Tick"/> is called, on the calling thread.
    /// </summary>
    internal sealed class TestCamera : IDisposable
    {
        /// <summary>
        /// A newly shown window is first seen on one tick and first captured 300 ms later.
        /// </summary>
        public const int TicksUntilNewWindowsAreCaptured = 4;

        private readonly ManualTimeProvider timeProvider = new();
        private readonly List<Exception> reportedExceptions = new();

        public TestCamera()
        {
            Camera = new(reportedExceptions.Add, timeProvider);
            Camera.Start();
        }

        public Camera Camera { get; }

        public Exception[] ReportedExceptions => reportedExceptions.ToArray();

        public void Dispose() => Camera.Dispose();

        /// <summary>
        /// Runs the given number of timer ticks, which are 100 ms apart.
        /// </summary>
        public void Tick(int count = 1)
        {
            for (var i = 0; i < count; i++)
                timeProvider.AdvanceToNextTimer();
        }

        public void TickUntilNewWindowsAreCaptured() => Tick(TicksUntilNewWindowsAreCaptured);

        public RecordedGif SaveGif()
        {
            return Camera.SaveGif() is { } bytes
                ? new RecordedGif(bytes)
                : throw new AssertionException("SaveGif returned null.");
        }

        /// <summary>
        /// The real mouse cursor is drawn into the GIF wherever it is, so this makes a test inconclusive if the cursor
        /// is over an area whose pixels the test checks.
        /// </summary>
        public static void AssumeCursorIsOutside(params TestWindow[] windows)
        {
            foreach (var window in windows)
            {
                var area = window.ClientBounds;
                area.Inflate(64, 64);
                if (area.Contains(Cursor.Position))
                    Assert.Inconclusive("The mouse cursor is over a test window.");
            }
        }
    }
}
