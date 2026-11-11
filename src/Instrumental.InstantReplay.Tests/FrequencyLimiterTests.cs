using System;
using NUnit.Framework;
using Shouldly;

namespace Instrumental.InstantReplay.Tests
{
    public static class FrequencyLimiterTests
    {
        private static FrequencyLimiter CreateLimiter(uint maximumCount, TimeSpan withinDuration)
        {
            return new(new(maximumCount, withinDuration), timestampFrequency: TimeSpan.TicksPerSecond);
        }

        private static long Timestamp(double seconds) => TimeSpan.FromSeconds(seconds).Ticks;

        [Test]
        public static void Allows_up_to_the_maximum_count_within_the_duration()
        {
            var limiter = CreateLimiter(maximumCount: 3, TimeSpan.FromHours(1));

            limiter.TryAddOccurrence(Timestamp(0)).ShouldBeTrue();
            limiter.TryAddOccurrence(Timestamp(1)).ShouldBeTrue();
            limiter.TryAddOccurrence(Timestamp(2)).ShouldBeTrue();
            limiter.TryAddOccurrence(Timestamp(3)).ShouldBeFalse();
            limiter.TryAddOccurrence(Timestamp(3599)).ShouldBeFalse();
        }

        [Test]
        public static void Allows_another_occurrence_once_the_oldest_one_is_a_full_duration_old()
        {
            var limiter = CreateLimiter(maximumCount: 3, TimeSpan.FromHours(1));
            limiter.TryAddOccurrence(Timestamp(0)).ShouldBeTrue();
            limiter.TryAddOccurrence(Timestamp(1)).ShouldBeTrue();
            limiter.TryAddOccurrence(Timestamp(2)).ShouldBeTrue();

            limiter.TryAddOccurrence(Timestamp(3600)).ShouldBeTrue();
            limiter.TryAddOccurrence(Timestamp(3600.5)).ShouldBeFalse();
            limiter.TryAddOccurrence(Timestamp(3601)).ShouldBeTrue();
        }

        [Test]
        public static void Occurrences_at_the_same_time_each_count()
        {
            var limiter = CreateLimiter(maximumCount: 2, TimeSpan.FromHours(1));

            limiter.TryAddOccurrence(Timestamp(5)).ShouldBeTrue();
            limiter.TryAddOccurrence(Timestamp(5)).ShouldBeTrue();
            limiter.TryAddOccurrence(Timestamp(5)).ShouldBeFalse();
        }

        [Test]
        public static void Refused_occurrences_do_not_count()
        {
            var limiter = CreateLimiter(maximumCount: 1, TimeSpan.FromSeconds(10));

            limiter.TryAddOccurrence(Timestamp(0)).ShouldBeTrue();
            limiter.TryAddOccurrence(Timestamp(9)).ShouldBeFalse();
            limiter.TryAddOccurrence(Timestamp(10)).ShouldBeTrue();
        }

        [Test]
        public static void Never_allows_occurrences_when_the_maximum_count_is_zero()
        {
            var limiter = CreateLimiter(maximumCount: 0, TimeSpan.FromHours(1));

            limiter.TryAddOccurrence(Timestamp(0)).ShouldBeFalse();
        }

        [Test]
        public static void Uses_the_given_timestamp_frequency()
        {
            var limiter = new FrequencyLimiter(new(maximumCount: 1, TimeSpan.FromSeconds(2)), timestampFrequency: 1000);

            limiter.TryAddOccurrence(0).ShouldBeTrue();
            limiter.TryAddOccurrence(1999).ShouldBeFalse();
            limiter.TryAddOccurrence(2000).ShouldBeTrue();
        }
    }
}
