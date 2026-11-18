using System;
using NUnit.Framework;
using WrongLine.Traffic;

namespace WrongLine.Crash.Tests
{
    public class CrashSettingsTests
    {
        private static float PlannerLength => new TrafficSettings().Layout.PlayerLengthMeters;

        [Test]
        public void TheDefaultsAreValidForTheDefaultTraffic()
        {
            Assert.DoesNotThrow(() => new CrashSettings().Validate(PlannerLength));
        }

        [Test]
        public void APlayerBoxLongerThanThePlannerAssumesIsRejected()
        {
            var settings = new CrashSettings();
            settings.Hitbox.PlayerHalfLength = PlannerLength * 0.5f + 0.1f;

            Assert.Throws<ArgumentException>(() => settings.Validate(PlannerLength));
        }

        [Test]
        public void GameOverBeforeTheSlowMotionHasEasedInIsRejected()
        {
            var settings = new CrashSettings();
            settings.SlowMotion.GameOverSeconds = settings.SlowMotion.EaseSeconds;

            Assert.Throws<ArgumentException>(() => settings.Validate(PlannerLength));
        }

        [Test]
        public void ASlowMotionScaleOfZeroIsRejected()
        {
            var settings = new CrashSettings();
            settings.SlowMotion.Scale = 0f;

            Assert.Throws<ArgumentException>(() => settings.Validate(PlannerLength));
        }

        [Test]
        public void ATrafficBoxBiggerThanTheCarIsRejected()
        {
            var settings = new CrashSettings();
            settings.Hitbox.TrafficScale = 1.2f;

            Assert.Throws<ArgumentException>(() => settings.Validate(PlannerLength));
        }

        [Test]
        public void NegativeSideswipeNumbersAreRejected()
        {
            var settings = new CrashSettings();
            settings.Sideswipe.PushMeters = -1f;

            Assert.Throws<ArgumentException>(() => settings.Validate(PlannerLength));
        }
    }
}
