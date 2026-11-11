using System;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Instrumental.InstantReplay.Tests
{
    public static class SharedResultMutexTests
    {
        [Test]
        public static void GetResult_returns_the_result_of_a_new_invocation_each_time()
        {
            var invocationCount = 0;
            var mutex = new SharedResultMutex<int>(() => ++invocationCount);

            mutex.GetResult().ShouldBe(1);
            mutex.GetResult().ShouldBe(2);
        }

        [Test]
        public static void Exceptions_are_thrown_and_do_not_affect_later_calls()
        {
            var shouldThrow = true;
            var mutex = new SharedResultMutex<int>(() => shouldThrow ? throw new InvalidOperationException("Test") : 1);

            Should.Throw<InvalidOperationException>(() => mutex.GetResult()).Message.ShouldBe("Test");

            shouldThrow = false;
            mutex.GetResult().ShouldBe(1);
        }

        [Test]
        public static void Concurrent_callers_share_a_single_invocation()
        {
            var invocationCount = 0;
            var waitingCallerResult = 0;
            var waitingCaller = (Thread?)null;

            var mutex = (SharedResultMutex<int>?)null;
            mutex = new SharedResultMutex<int>(() =>
            {
                invocationCount++;

                waitingCaller = new Thread(() => waitingCallerResult = mutex!.GetResult());
                waitingCaller.Start();

                // Wait until the other caller is blocked waiting for this invocation's result.
                while ((waitingCaller.ThreadState & ThreadState.WaitSleepJoin) == 0)
                    Thread.Sleep(1);

                return 42;
            });

            mutex.GetResult().ShouldBe(42);
            waitingCaller!.Join();

            waitingCallerResult.ShouldBe(42);
            invocationCount.ShouldBe(1);
        }
    }
}
