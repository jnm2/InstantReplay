using System;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Instrumental.InstantReplay.Tests
{
    public static class BasicCompletionSourceTests
    {
        [Test]
        public static void GetResult_returns_the_result()
        {
            using var source = new BasicCompletionSource<int>();
            source.SetResult(42);

            source.GetResult().ShouldBe(42);
        }

        [Test]
        public static void GetResult_throws_the_exception()
        {
            using var source = new BasicCompletionSource<int>();
            var exception = new InvalidOperationException("Test");
            source.SetException(exception);

            Should.Throw<InvalidOperationException>(() => source.GetResult()).ShouldBeSameAs(exception);
        }

        [Test]
        public static void Completing_twice_throws()
        {
            using var source = new BasicCompletionSource<int>();
            source.SetResult(1);

            Should.Throw<InvalidOperationException>(() => source.SetResult(2));
            Should.Throw<InvalidOperationException>(() => source.SetException(new InvalidOperationException()));
        }

        [Test]
        public static void SetExceptionAndReturnFalse_completes_and_returns_false()
        {
            using var source = new BasicCompletionSource<int>();
            var exception = new InvalidOperationException("Test");

            source.SetExceptionAndReturnFalse(exception).ShouldBeFalse();
            Should.Throw<InvalidOperationException>(() => source.GetResult()).ShouldBeSameAs(exception);
        }

        [Test]
        public static void GetResult_waits_for_completion_on_another_thread()
        {
            using var source = new BasicCompletionSource<int>();
            var thread = new Thread(() =>
            {
                Thread.Sleep(50);
                source.SetResult(7);
            });
            thread.Start();

            source.GetResult().ShouldBe(7);
            thread.Join();
        }
    }
}
