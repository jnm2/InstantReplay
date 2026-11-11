using System;
using System.Collections.Generic;
using System.Threading;

namespace Instrumental.InstantReplay.Tests
{
    /// <summary>
    /// A <see cref="TimeProvider"/> whose time only moves when the test moves it. Timer callbacks run synchronously on
    /// the thread that moves time forward.
    /// </summary>
    internal sealed class ManualTimeProvider : TimeProvider
    {
        private static readonly TimeSpan Infinite = TimeSpan.FromMilliseconds(-1);

        private readonly List<ManualTimer> timers = new();
        private long timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => timestamp;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state);
            timers.Add(timer);
            timer.Change(dueTime, period);
            return timer;
        }

        /// <summary>
        /// Moves time forward to the next time a timer is due and runs that timer's callback.
        /// </summary>
        public void AdvanceToNextTimer()
        {
            var nextTimer = (ManualTimer?)null;

            foreach (var timer in timers)
            {
                if (timer.DueTimestamp is { } dueTimestamp && (nextTimer is null || dueTimestamp < nextTimer.DueTimestamp))
                    nextTimer = timer;
            }

            if (nextTimer is null)
                throw new InvalidOperationException("No timer is scheduled.");

            timestamp = nextTimer.DueTimestamp!.Value;
            nextTimer.Fire();
        }

        private sealed class ManualTimer : ITimer
        {
            private readonly ManualTimeProvider owner;
            private readonly TimerCallback callback;
            private readonly object? state;
            private TimeSpan period;

            public ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state)
            {
                this.owner = owner;
                this.callback = callback;
                this.state = state;
            }

            public long? DueTimestamp { get; private set; }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                this.period = period;
                DueTimestamp = dueTime == Infinite ? null : owner.timestamp + dueTime.Ticks;
                return true;
            }

            public void Fire()
            {
                DueTimestamp = period == Infinite || period == TimeSpan.Zero ? null : DueTimestamp + period.Ticks;
                callback.Invoke(state);
            }

            public void Dispose()
            {
                DueTimestamp = null;
                owner.timers.Remove(this);
            }

#if !NETFRAMEWORK
            public System.Threading.Tasks.ValueTask DisposeAsync()
            {
                Dispose();
                return default;
            }
#endif
        }
    }
}
