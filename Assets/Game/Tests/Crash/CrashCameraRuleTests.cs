using NUnit.Framework;

namespace WrongLine.Crash.Tests
{
    public class CrashCameraRuleTests
    {
        private const float Frame = 1f / 60f;

        private CrashSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new CrashSettings();
        }

        [Test]
        public void ItFollowsAWreckThrownForwardWithoutFallingFarBehind()
        {
            var rule = new CrashCameraRule(_settings.Camera, 100.0);
            double wreck = 100.0;
            double last = rule.Distance;
            double worstLag = 0.0;

            for (int i = 0; i < 180; i++)
            {
                wreck += 30.0 * Frame;
                rule.Update(wreck, Frame);
                Assert.GreaterOrEqual(rule.Distance, last, "it only goes forward while the wreck does");
                last = rule.Distance;
                worstLag = System.Math.Max(worstLag, wreck - rule.Distance);
            }

            // A steady chase lags by speed / sharpness (10 m at 3 per second); a little more for the start.
            Assert.LessOrEqual(worstLag, 30.0 / _settings.Camera.FollowSharpness + 2.0);
        }

        [Test]
        public void ItComesBackWhenTheWreckBouncesBack()
        {
            var rule = new CrashCameraRule(_settings.Camera, 100.0);
            for (int i = 0; i < 120; i++)
            {
                rule.Update(140.0, Frame);
            }

            double forward = rule.Distance;
            for (int i = 0; i < 120; i++)
            {
                rule.Update(120.0, Frame);
            }

            Assert.Greater(forward, 135.0);
            Assert.Less(rule.Distance, forward);
            Assert.AreEqual(120.0, rule.Distance, 1.0);
        }

        [Test]
        public void ItSettlesOnAStillWreckWithoutOvershooting()
        {
            var rule = new CrashCameraRule(_settings.Camera, 100.0);

            for (int i = 0; i < 600; i++)
            {
                rule.Update(130.0, Frame);
                Assert.LessOrEqual(rule.Distance, 130.0);
            }

            Assert.AreEqual(130.0, rule.Distance, 1e-3);
        }

        [Test]
        public void APausedFrameLeavesItWhereItIs()
        {
            var rule = new CrashCameraRule(_settings.Camera, 100.0);

            rule.Update(200.0, 0f);

            Assert.AreEqual(100.0, rule.Distance);
        }

        [Test]
        public void TheFrameRateDoesNotChangeWhereItGets()
        {
            var fast = new CrashCameraRule(_settings.Camera, 0.0);
            var slow = new CrashCameraRule(_settings.Camera, 0.0);

            for (int i = 0; i < 60; i++)
            {
                fast.Update(50.0, 1f / 60f);
            }

            for (int i = 0; i < 6; i++)
            {
                slow.Update(50.0, 1f / 6f);
            }

            Assert.AreEqual(fast.Distance, slow.Distance, 1e-3);
        }
    }
}
