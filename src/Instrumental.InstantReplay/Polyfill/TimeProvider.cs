#if !NET8_0_OR_GREATER
using System;
using System.Diagnostics;
using System.Threading;
using Instrumental.InstantReplay;

namespace System
{
    /// <summary>
    /// The subset of .NET 8's <c>System.TimeProvider</c> that this library uses.
    /// </summary>
    internal class TimeProvider
    {
        protected TimeProvider()
        {
        }

        public static TimeProvider System { get; } = new();

        public virtual long TimestampFrequency => Stopwatch.Frequency;

        public virtual long GetTimestamp() => Stopwatch.GetTimestamp();

        public virtual ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            if (callback is null) throw new ArgumentNullException(nameof(callback));

            return new SystemTimer(new Timer(callback, state, dueTime, period));
        }
    }
}

namespace System.Threading
{
    /// <summary>
    /// The subset of .NET 8's <c>System.Threading.ITimer</c> that this library uses.
    /// </summary>
    internal interface ITimer : IDisposable
    {
        bool Change(TimeSpan dueTime, TimeSpan period);
    }
}

namespace Instrumental.InstantReplay
{
    internal sealed class SystemTimer : ITimer
    {
        private readonly Timer timer;

        public SystemTimer(Timer timer)
        {
            this.timer = timer;
        }

        public bool Change(TimeSpan dueTime, TimeSpan period) => timer.Change(dueTime, period);

        public void Dispose() => timer.Dispose();
    }
}
#endif
