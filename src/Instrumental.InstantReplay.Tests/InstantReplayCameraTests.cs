using System;
using NUnit.Framework;
using Shouldly;

namespace Instrumental.InstantReplay.Tests
{
    public static class InstantReplayCameraTests
    {
        [Test]
        public static void SaveGif_with_no_frames_collected_should_return_null()
        {
            InstantReplayCamera.SaveGif().ShouldBeNull();
        }

        [Test]
        public static void Start_requires_a_callback()
        {
            Should.Throw<ArgumentNullException>(() => InstantReplayCamera.Start(null!))
                .ParamName.ShouldBe("reportBackgroundException");
        }
    }
}
