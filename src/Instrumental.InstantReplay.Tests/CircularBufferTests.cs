using NUnit.Framework;
using Shouldly;

namespace Instrumental.InstantReplay.Tests
{
    public static class CircularBufferTests
    {
        [Test]
        public static void Count_grows_until_the_capacity_is_reached()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Count.ShouldBe(0);

            buffer.Add(1);
            buffer.Add(2);
            buffer.Count.ShouldBe(2);

            buffer.Add(3);
            buffer.Add(4);
            buffer.Count.ShouldBe(3);
            buffer.Capacity.ShouldBe(3);
        }

        [Test]
        public static void ToArray_returns_items_from_oldest_to_newest()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.ToArray().ShouldBeEmpty();

            buffer.Add(1);
            buffer.Add(2);
            buffer.ToArray().ShouldBe(new[] { 1, 2 });

            buffer.Add(3);
            buffer.ToArray().ShouldBe(new[] { 1, 2, 3 });

            buffer.Add(4);
            buffer.Add(5);
            buffer.ToArray().ShouldBe(new[] { 3, 4, 5 });
        }

        [Test]
        public static void GetNextRef_returns_the_slot_of_the_oldest_item_once_full()
        {
            var buffer = new CircularBuffer<string>(2);
            buffer.GetNextRef().ShouldBeNull();
            buffer.GetNextRef().ShouldBeNull();

            buffer.Add("a");
            buffer.Add("b");

            ref var slot = ref buffer.GetNextRef();
            slot.ShouldBe("a");
            slot = "c";

            buffer.ToArray().ShouldBe(new[] { "b", "c" });
        }
    }
}
