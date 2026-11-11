using System;
using System.Drawing;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Shouldly;

namespace Instrumental.InstantReplay.Tests
{
    public static class CameraTests
    {
        private static readonly Color Black = Color.FromArgb(0, 0, 0);
        private static readonly Color Red = Color.FromArgb(255, 0, 0);
        private static readonly Color Green = Color.FromArgb(0, 255, 0);
        private static readonly Color Blue = Color.FromArgb(0, 0, 255);
        private static readonly Color White = Color.FromArgb(255, 255, 255);

        [Test]
        public static void SaveGif_returns_null_when_no_windows_have_been_captured()
        {
            using var camera = new TestCamera();
            camera.Tick(10);

            camera.Camera.SaveGif().ShouldBeNull();
        }

        [Test]
        public static void New_windows_are_first_captured_300_ms_after_they_appear()
        {
            using var camera = new TestCamera();
            camera.Tick();

            using var window = new TestWindow(new Rectangle(300, 300, 60, 40), Red);

            camera.Tick(TestCamera.TicksUntilNewWindowsAreCaptured - 1);
            camera.Camera.SaveGif().ShouldBeNull();

            camera.Tick();
            camera.Camera.SaveGif().ShouldNotBeNull();
        }

        [Test]
        public static void Window_contents_are_captured()
        {
            using var camera = new TestCamera();
            using var window = new TestWindow(new Rectangle(300, 300, 60, 40), Blue);
            window.Fill(new Rectangle(10, 10, 20, 15), Red);
            TestCamera.AssumeCursorIsOutside(window);

            camera.TickUntilNewWindowsAreCaptured();

            using var gif = camera.SaveGif();
            camera.ReportedExceptions.ShouldBeEmpty();
            gif.Size.ShouldBe(window.ClientBounds.Size);
            gif.FrameCount.ShouldBe(1);
            gif.GetPixel(0, 5, 5).ShouldBeColor(Blue);
            gif.GetPixel(0, 10, 10).ShouldBeColor(Red);
            gif.GetPixel(0, 29, 24).ShouldBeColor(Red);
            gif.GetPixel(0, 30, 25).ShouldBeColor(Blue);
            gif.GetPixel(0, 59, 39).ShouldBeColor(Blue);
        }

        [Test]
        public static void Gif_spans_all_captured_windows_at_their_screen_positions()
        {
            using var camera = new TestCamera();
            using var first = new TestWindow(new Rectangle(300, 300, 40, 30), Red);
            using var second = new TestWindow(new Rectangle(400, 350, 50, 20), Green);
            TestCamera.AssumeCursorIsOutside(first, second);

            camera.TickUntilNewWindowsAreCaptured();

            using var gif = camera.SaveGif();
            gif.Size.ShouldBe(new Size(150, 70));
            gif.GetPixel(0, 0, 0).ShouldBeColor(Red);
            gif.GetPixel(0, 39, 29).ShouldBeColor(Red);
            gif.GetPixel(0, 100, 50).ShouldBeColor(Green);
            gif.GetPixel(0, 149, 69).ShouldBeColor(Green);
            gif.GetPixel(0, 70, 10).ShouldBeColor(Black);
        }

        [Test]
        public static void Windows_are_drawn_in_their_current_z_order()
        {
            using var camera = new TestCamera();
            using var red = new TestWindow(new Rectangle(300, 300, 60, 60), Red);
            using var green = new TestWindow(new Rectangle(330, 330, 60, 60), Green);
            TestCamera.AssumeCursorIsOutside(red, green);

            green.PlaceAbove(red);
            camera.TickUntilNewWindowsAreCaptured();
            red.PlaceAbove(green);
            camera.Tick();

            using var gif = camera.SaveGif();
            gif.FrameCount.ShouldBe(2);
            gif.GetPixel(0, 45, 45).ShouldBeColor(Green);
            gif.GetPixel(1, 45, 45).ShouldBeColor(Red);
        }

        [Test]
        public static void Hidden_windows_are_not_drawn()
        {
            using var camera = new TestCamera();
            using var window = new TestWindow(new Rectangle(300, 300, 60, 40), Red);
            TestCamera.AssumeCursorIsOutside(window);

            camera.TickUntilNewWindowsAreCaptured();
            window.Hide();
            camera.Tick();

            using var gif = camera.SaveGif();
            gif.FrameCount.ShouldBe(2);
            gif.GetPixel(0, 30, 20).ShouldBeColor(Red);
            gif.GetPixel(1, 30, 20).ShouldBeColor(Black);
        }

        [Test]
        public static void Windows_shown_again_are_captured_right_away()
        {
            using var camera = new TestCamera();
            using var window = new TestWindow(new Rectangle(300, 300, 60, 40), Red);
            TestCamera.AssumeCursorIsOutside(window);

            camera.TickUntilNewWindowsAreCaptured();
            window.Hide();
            camera.Tick(5);
            window.Show();
            window.Fill(Green);
            camera.Tick();

            using var gif = camera.SaveGif();
            gif.FrameCount.ShouldBe(3);
            gif.GetPixel(0, 30, 20).ShouldBeColor(Red);
            gif.GetPixel(1, 30, 20).ShouldBeColor(Black);
            gif.GetPixel(2, 30, 20).ShouldBeColor(Green);
        }

        [Test]
        public static void Moved_windows_are_drawn_at_their_new_position()
        {
            using var camera = new TestCamera();
            using var window = new TestWindow(new Rectangle(300, 300, 40, 30), Red);

            camera.TickUntilNewWindowsAreCaptured();
            window.Location = new Point(360, 340);
            TestCamera.AssumeCursorIsOutside(window);
            camera.Tick();

            using var gif = camera.SaveGif();
            gif.Size.ShouldBe(new Size(100, 70));
            gif.FrameCount.ShouldBe(2);
            gif.GetPixel(0, 10, 10).ShouldBeColor(Red);
            gif.GetPixel(0, 90, 60).ShouldBeColor(Black);
            gif.GetPixel(1, 10, 10).ShouldBeColor(Black);
            gif.GetPixel(1, 90, 60).ShouldBeColor(Red);
        }

        [Test]
        public static void Resized_windows_are_drawn_at_their_new_size()
        {
            using var camera = new TestCamera();
            using var window = new TestWindow(new Rectangle(300, 300, 40, 30), Red);
            TestCamera.AssumeCursorIsOutside(window);

            camera.TickUntilNewWindowsAreCaptured();
            window.Size = new Size(300, 200);
            window.Fill(Green);
            TestCamera.AssumeCursorIsOutside(window);
            camera.Tick();
            window.Size = new Size(20, 10);
            window.Fill(Blue);
            camera.Tick();

            using var gif = camera.SaveGif();
            gif.Size.ShouldBe(new Size(300, 200));
            gif.FrameCount.ShouldBe(3);
            gif.GetPixel(0, 39, 29).ShouldBeColor(Red);
            gif.GetPixel(0, 299, 199).ShouldBeColor(Black);
            gif.GetPixel(1, 299, 199).ShouldBeColor(Green);
            gif.GetPixel(2, 19, 9).ShouldBeColor(Blue);
            gif.GetPixel(2, 20, 10).ShouldBeColor(Black);
        }

        [Test]
        public static void Windows_that_grow_after_the_history_is_full_are_captured_at_their_new_size()
        {
            using var camera = new TestCamera();
            using var window = new TestWindow(new Rectangle(300, 300, 40, 30), Red);

            camera.TickUntilNewWindowsAreCaptured();
            camera.Tick(100); // Every frame in the history now has a bitmap sized for the original window

            window.Size = new Size(300, 200);
            window.Fill(Green);
            TestCamera.AssumeCursorIsOutside(window);
            camera.Tick();

            using var gif = camera.SaveGif();
            gif.Size.ShouldBe(new Size(300, 200));
            var lastFrame = gif.FrameCount - 1;
            gif.GetPixel(lastFrame, 0, 0).ShouldBeColor(Green);
            gif.GetPixel(lastFrame, 299, 199).ShouldBeColor(Green);
        }

        [Test]
        public static void Frame_delays_match_the_time_between_visible_changes()
        {
            using var camera = new TestCamera();
            using var window = new TestWindow(new Rectangle(300, 300, 60, 40), Red);
            TestCamera.AssumeCursorIsOutside(window);

            camera.TickUntilNewWindowsAreCaptured();
            window.Fill(Green);
            camera.Tick();
            window.Fill(Blue);
            camera.Tick(3);
            window.Fill(White);
            camera.Tick();

            using var gif = camera.SaveGif();
            gif.FrameCount.ShouldBe(4);
            gif.GetPixel(0, 30, 20).ShouldBeColor(Red);
            gif.GetPixel(1, 30, 20).ShouldBeColor(Green);
            gif.GetPixel(2, 30, 20).ShouldBeColor(Blue);
            gif.GetPixel(3, 30, 20).ShouldBeColor(White);
            gif.GetDelayInHundredthsOfASecond(0).ShouldBe(10);
            gif.GetDelayInHundredthsOfASecond(1).ShouldBe(10);
            gif.GetDelayInHundredthsOfASecond(2).ShouldBe(30);
            gif.GetDelayInHundredthsOfASecond(3).ShouldBe(400);
        }

        [Test]
        public static void Ticks_without_visible_changes_do_not_add_frames()
        {
            using var camera = new TestCamera();
            using var window = new TestWindow(new Rectangle(300, 300, 60, 40), Red);
            TestCamera.AssumeCursorIsOutside(window);

            camera.TickUntilNewWindowsAreCaptured();
            camera.Tick(20);

            using var gif = camera.SaveGif();
            gif.FrameCount.ShouldBe(1);
            gif.GetDelayInHundredthsOfASecond(0).ShouldBe(400);
        }

        [Test]
        public static void Gif_covers_the_last_100_ticks()
        {
            using var camera = new TestCamera();
            using var window = new TestWindow(new Rectangle(300, 300, 60, 40), Red);
            TestCamera.AssumeCursorIsOutside(window);

            camera.TickUntilNewWindowsAreCaptured();
            camera.Tick(10);
            window.Fill(Green);
            camera.Tick(99);

            using (var gif = camera.SaveGif())
                gif.GetPixel(0, 30, 20).ShouldBeColor(Red);

            camera.Tick();

            using (var gif = camera.SaveGif())
            {
                gif.FrameCount.ShouldBe(1);
                gif.GetPixel(0, 30, 20).ShouldBeColor(Green);
            }
        }

        [Test]
        public static void Closed_windows_remain_in_the_gif_for_100_ticks()
        {
            using var camera = new TestCamera();
            using var window = new TestWindow(new Rectangle(300, 300, 60, 40), Red);
            TestCamera.AssumeCursorIsOutside(window);

            camera.TickUntilNewWindowsAreCaptured();
            window.Dispose();
            camera.Tick(99);

            using (var gif = camera.SaveGif())
                gif.GetPixel(0, 30, 20).ShouldBeColor(Red);

            camera.Tick();

            camera.Camera.SaveGif().ShouldBeNull();
        }

        [Test]
        public static void Disposing_releases_all_GDI_objects()
        {
            using var window = new TestWindow(new Rectangle(300, 300, 60, 40), Red);
            var camera = new TestCamera();
            var baseline = GetGdiObjectCount();

            camera.TickUntilNewWindowsAreCaptured();
            camera.Tick(20);
            GetGdiObjectCount().ShouldBeGreaterThan(baseline + 20);

            camera.Dispose();
            GetGdiObjectCount().ShouldBeLessThanOrEqualTo(baseline);
            camera.Camera.SaveGif().ShouldBeNull();
        }

        private static uint GetGdiObjectCount() => GetGuiResources(GetCurrentProcess(), uiFlags: 0);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("user32.dll")]
        private static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);
    }
}
