using System;
using NUnit.Framework;

namespace WrongLine.Traffic.Tests
{
    public class TrafficHeadlightRuleTests
    {
        private static TrafficSettings.LightsGroup Group(int maxLevel = 0)
        {
            return new TrafficSettings.LightsGroup { IntensityScale = 0.3f, RangeScale = 0.5f, MaxLevel = maxLevel };
        }

        [Test]
        public void ByDay_TheLightIsOff_AtNightItIsAShareOfThePlayers()
        {
            Assert.AreEqual(0f, TrafficHeadlightRule.Intensity(0f, 0, Group(), 45f));
            Assert.AreEqual(13.5f, TrafficHeadlightRule.Intensity(1f, 0, Group(), 45f), 1e-4f);
            Assert.AreEqual(6.75f, TrafficHeadlightRule.Intensity(0.5f, 0, Group(), 45f), 1e-4f, "It fades in with the night.");
            Assert.Less(TrafficHeadlightRule.Intensity(1f, 0, Group(), 45f), 45f, "The player's lights stay the strongest.");
        }

        [Test]
        public void OnlyTheLevelsUpToTheLimit_CastLight()
        {
            Assert.AreEqual(0f, TrafficHeadlightRule.Intensity(1f, 1, Group(0), 45f));
            Assert.Greater(TrafficHeadlightRule.Intensity(1f, 1, Group(1), 45f), 0f);
            Assert.AreEqual(0f, TrafficHeadlightRule.Intensity(1f, 2, Group(1), 45f));
            Assert.AreEqual(0f, TrafficHeadlightRule.Intensity(1f, TrafficLodRule.None, Group(2), 45f), "A car that is not out has no light.");
        }

        [Test]
        public void TheRange_IsAShareOfThePlayers_AndTheSettingsAreChecked()
        {
            Assert.AreEqual(25f, TrafficHeadlightRule.Range(50f, Group()), 1e-4f);

            var settings = new TrafficSettings();
            settings.Lights.MaxLevel = TrafficCarView.LevelCount;
            Assert.Throws<ArgumentException>(() => settings.Validate(60f, 400f), "MaxLevel past the last level.");
            settings.Lights.MaxLevel = 0;
            settings.Lights.RangeScale = 0f;
            Assert.Throws<ArgumentException>(() => settings.Validate(60f, 400f), "A light with no reach.");
        }
    }
}
