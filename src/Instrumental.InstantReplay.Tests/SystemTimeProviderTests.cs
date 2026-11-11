using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Instrumental.InstantReplay.Tests
{
    public static class SystemTimeProviderTests
    {
        [Test]
        public static void Timestamps_are_Stopwatch_timestamps()
        {
            TimeProvider.System.TimestampFrequency.ShouldBe(Stopwatch.Frequency);

            var before = Stopwatch.GetTimestamp();
            var timestamp = TimeProvider.System.GetTimestamp();
            var after = Stopwatch.GetTimestamp();

            timestamp.ShouldBeInRange(before, after);
        }

        [Test]
        public static void Timer_invokes_the_callback_with_the_state()
        {
            using var invoked = new ManualResetEvent(initialState: false);
            var receivedState = (object?)null;
            var state = new object();

            using var timer = TimeProvider.System.CreateTimer(
                s =>
                {
                    receivedState = s;
                    invoked.Set();
                },
                state,
                dueTime: TimeSpan.Zero,
                period: TimeSpan.FromMilliseconds(-1));

            invoked.WaitOne(TimeSpan.FromSeconds(10)).ShouldBeTrue();
            receivedState.ShouldBeSameAs(state);
        }

        [Test]
        public static void Timer_can_be_rescheduled()
        {
            using var invoked = new ManualResetEvent(initialState: false);

            using var timer = TimeProvider.System.CreateTimer(
                _ => invoked.Set(),
                state: null,
                dueTime: TimeSpan.FromMilliseconds(-1),
                period: TimeSpan.FromMilliseconds(-1));

            invoked.WaitOne(TimeSpan.FromMilliseconds(100)).ShouldBeFalse();

            timer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(-1)).ShouldBeTrue();
            invoked.WaitOne(TimeSpan.FromSeconds(10)).ShouldBeTrue();
        }
    }
}
