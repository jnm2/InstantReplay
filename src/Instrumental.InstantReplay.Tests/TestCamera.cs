using System;
using System.Collections.Generic;
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
    }
}
