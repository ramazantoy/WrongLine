using NUnit.Framework;
using WrongLine.Audio;

namespace WrongLine.Tests.Audio
{
    public sealed class EngineSoundRuleTests
    {
        private const float Frame = 1f / 60f;

        private static EngineSoundRule Rule(out EngineSoundSettings settings)
        {
            settings = new EngineSoundSettings();
            return new EngineSoundRule(settings);
        }

        private static void Run(EngineSoundRule rule, float seconds, float speed, float lateral = 0f, float scale = 1f, bool crashed = false)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                rule.Update(Frame, speed, lateral, scale, crashed);
            }
        }

        [Test]
        public void GearsFollowTheTopSpeeds_AndTheLastGearHoldsAboveTheTop()
        {
            var rule = Rule(out var settings);

            Assert.AreEqual(0, rule.GearAt(0f));
            Assert.AreEqual(0, rule.GearAt(settings.GearTopSpeeds[0] - 0.01f));
            Assert.AreEqual(1, rule.GearAt(settings.GearTopSpeeds[0]));
            Assert.AreEqual(settings.GearTopSpeeds.Length - 1, rule.GearAt(1000f));
        }

        [Test]
        public void PitchRisesWithTheSpeedInsideAGear()
        {
            var rule = Rule(out var settings);
            float speed = settings.GearTopSpeeds[0] + 0.5f;
            rule.Update(Frame, speed, 0f, 1f, false);
            float previous = rule.Sound.Pitch;

            for (int i = 0; i < 30; i++)
            {
                speed += 0.1f;
                rule.Update(Frame, speed, 0f, 1f, false);
                Assert.GreaterOrEqual(rule.Sound.Pitch, previous);
                previous = rule.Sound.Pitch;
            }

            Assert.Greater(previous, 0f);
        }

        [Test]
        public void PitchFallsWhenTheCarShiftsUp_AndTheFallTakesTime()
        {
            var rule = Rule(out var settings);
            float top = settings.GearTopSpeeds[0];
            Run(rule, 1f, top - 0.05f);
            float beforeShift = rule.Sound.Pitch;

            rule.Update(Frame, top + 0.05f, 0f, 1f, false);
            float justAfter = rule.Sound.Pitch;
            Run(rule, 1f, top + 0.05f);
            float settled = rule.Sound.Pitch;

            Assert.Less(justAfter, beforeShift, "the revs start to fall at once");
            Assert.Greater(justAfter, settled, "but not in one frame: that is the dip you hear");
        }

        [Test]
        public void TopGearAtTopSpeedHoldsFullRevs()
        {
            var rule = Rule(out var settings);

            Run(rule, 3f, settings.GearTopSpeeds[settings.GearTopSpeeds.Length - 1]);

            Assert.AreEqual(1f, rule.Sound.Revs, 1e-4f);
            Assert.AreEqual(settings.PitchAtRedline, rule.Sound.Pitch, 1e-4f);
            Assert.AreEqual(0f, rule.Sound.LowVolume, 1e-4f, "only the high loop at full revs");
        }

        [Test]
        public void SlowMotionLowersThePitch()
        {
            var rule = Rule(out _);
            Run(rule, 1f, 30f);
            float normal = rule.Sound.Pitch;

            Run(rule, 0.1f, 30f, scale: 0.25f);

            Assert.AreEqual(normal * 0.25f, rule.Sound.Pitch, 1e-4f);
        }

        [Test]
        public void ACrashLetsTheEngineAndTheWindDieAway()
        {
            var rule = Rule(out var settings);
            Run(rule, 1f, 30f);
            Assert.Greater(rule.Sound.LowVolume + rule.Sound.HighVolume, 0.1f);

            Run(rule, settings.CrashFadeSeconds + 0.1f, 30f, crashed: true);

            Assert.AreEqual(0f, rule.Sound.LowVolume + rule.Sound.HighVolume, 1e-4f);
            Assert.AreEqual(0f, rule.Sound.WindVolume, 1e-4f);
        }

        [Test]
        public void ChangingLanesAddsAFewRevs()
        {
            var steady = Rule(out _);
            var swerving = Rule(out _);

            Run(steady, 1f, 25f);
            Run(swerving, 1f, 25f, lateral: 6f);

            Assert.Greater(swerving.Sound.Revs, steady.Sound.Revs);
        }

        [Test]
        public void TheLoopsCrossfadeAndTheWindGrowsWithSpeed()
        {
            var rule = Rule(out _);
            Run(rule, 1f, 5f);
            EngineSound slow = rule.Sound;
            Run(rule, 2f, 41f);
            EngineSound fast = rule.Sound;

            Assert.Greater(slow.LowVolume, slow.HighVolume);
            Assert.Greater(fast.HighVolume, fast.LowVolume);
            Assert.Greater(fast.WindVolume, slow.WindVolume);
        }

        [Test]
        public void TheDefaultSettingsAreValid()
        {
            Assert.DoesNotThrow(() => new EngineSoundSettings().Validate());
            Assert.Throws<System.ArgumentException>(() => new EngineSoundSettings { GearTopSpeeds = new[] { 20f, 10f } }.Validate());
        }
    }
}
