using System;
using NUnit.Framework;

namespace WrongLine.World.Tests
{
    public class LightSwitchRuleTests
    {
        private static LightSwitchRule Rule(float on = 0.6f, float off = 0.4f, float fade = 1f)
        {
            return new LightSwitchRule(new LightSwitchSettings(on, off, fade));
        }

        [Test]
        public void InFullDay_TheLightsAreOff()
        {
            LightSwitchRule rule = Rule();

            rule.Update(0f, 0.1f);
            rule.Update(0f, 10f);

            Assert.IsFalse(rule.On);
            Assert.AreEqual(0f, rule.Level);
        }

        [Test]
        public void WhenTheDarknessReachesTheOnThreshold_TheLightsTurnOnAndFadeToFull()
        {
            LightSwitchRule rule = Rule(fade: 1f);
            rule.Update(0f, 0.1f);

            rule.Update(0.6f, 0.1f);
            Assert.IsTrue(rule.On);
            Assert.That(rule.Level, Is.GreaterThan(0f).And.LessThan(1f), "Fading in, not at once.");

            rule.Update(0.6f, 5f);
            Assert.AreEqual(1f, rule.Level);
        }

        [Test]
        public void BetweenTheThresholds_TheLightsKeepTheirState_FromBothSides()
        {
            LightSwitchRule rule = Rule(on: 0.6f, off: 0.4f);
            rule.Update(0f, 0.1f);

            rule.Update(0.5f, 5f);
            Assert.IsFalse(rule.On, "Darkening, not yet at the on threshold: still off.");

            rule.Update(0.7f, 5f);
            Assert.IsTrue(rule.On);

            rule.Update(0.5f, 5f);
            Assert.IsTrue(rule.On, "Brightening, not yet at the off threshold: still on.");
            Assert.AreEqual(1f, rule.Level);

            rule.Update(0.4f, 5f);
            Assert.IsFalse(rule.On);
            Assert.AreEqual(0f, rule.Level);
        }

        [Test]
        public void TheLevel_IsHalfWayHalfWayThroughTheFade_AndEasesAtBothEnds()
        {
            LightSwitchRule rule = Rule(fade: 2f);
            rule.Update(0f, 0.1f);
            rule.Update(1f, 0f);

            rule.Update(1f, 1f);
            Assert.AreEqual(0.5f, rule.Level, 1e-4f);

            LightSwitchRule slow = Rule(fade: 10f);
            slow.Update(0f, 0.1f);
            slow.Update(1f, 1f);
            Assert.Less(slow.Level, 0.1f, "A tenth of the way in, the easing is still under a tenth of the light (smoothstep: 0.028).");
        }

        [Test]
        public void TheFirstUpdate_PutsTheLightsStraightInTheirState()
        {
            LightSwitchRule night = Rule();
            night.Update(1f, 0.016f);
            Assert.IsTrue(night.On);
            Assert.AreEqual(1f, night.Level, "A run that begins at night starts lit, not fading in.");

            LightSwitchRule day = Rule();
            day.Update(0.1f, 0.016f);
            Assert.AreEqual(0f, day.Level);
        }

        [Test]
        public void WithNoTimePassing_TheFadeHolds()
        {
            LightSwitchRule rule = Rule(fade: 1f);
            rule.Update(0f, 0.1f);
            rule.Update(1f, 0.5f);
            float before = rule.Level;

            rule.Update(1f, 0f);
            rule.Update(1f, 0f);

            Assert.AreEqual(before, rule.Level, "A pause freezes the fade.");
        }

        [Test]
        public void TurningRound_MidFade_FadesBackFromWhereItWas()
        {
            LightSwitchRule rule = Rule(fade: 1f);
            rule.Update(0f, 0.1f);
            rule.Update(1f, 0.5f);
            float peak = rule.Level;

            rule.Update(0f, 0.25f);

            Assert.Less(rule.Level, peak);
            Assert.Greater(rule.Level, 0f, "Not a jump to off.");
        }

        [TestCase(0.6f, 0.4f, 1f)]
        [TestCase(0.5f, 0.5f, 1f)]
        [TestCase(-0.1f, 0.6f, 1f)]
        [TestCase(0.4f, 1.2f, 1f)]
        [TestCase(0.4f, 0.6f, 0f)]
        public void Validate_RejectsThresholdsAndFadesThatMakeNoSense(float off, float on, float fade)
        {
            Assert.Throws<ArgumentException>(() => new LightSwitchRule(new LightSwitchSettings(on, off, fade)));
        }

        [Test]
        public void Validate_AcceptsTheDefaults()
        {
            Assert.DoesNotThrow(() => new LightsSettings().Validate());
        }
    }
}
