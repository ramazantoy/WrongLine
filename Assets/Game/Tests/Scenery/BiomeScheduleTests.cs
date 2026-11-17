using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace WrongLine.Scenery.Tests
{
    public class BiomeScheduleTests
    {
        private static readonly BiomeSettings _settings = new BiomeSettings(1500f, 3000f, 150f);

        private static List<BiomeTransition> Transitions(BiomeSchedule schedule, double upTo)
        {
            var found = new List<BiomeTransition>();
            double from = 0.0;
            while (schedule.TryTransition(from, upTo, out BiomeTransition transition))
            {
                found.Add(transition);
                from = transition.Distance + schedule.BlendMeters; // Past this band.
            }

            return found;
        }

        [Test]
        public void TheSameSeed_GivesTheSameBiomes_AnotherSeedOthers()
        {
            var a = new BiomeSchedule(42, 4, _settings);
            var b = new BiomeSchedule(42, 4, _settings);
            var c = new BiomeSchedule(43, 4, _settings);

            bool differs = false;
            for (double d = 0.0; d < 60000.0; d += 97.0)
            {
                Assert.AreEqual(a.BiomeAt(d), b.BiomeAt(d), $"at {d}");
                differs |= a.BiomeAt(d) != c.BiomeAt(d);
            }

            Assert.IsTrue(differs, "Another seed gives another road of biomes.");
        }

        [Test]
        public void EveryStretch_IsAsLongAsConfigured_AndNeverTheSameBiomeTwiceInARow()
        {
            var schedule = new BiomeSchedule(7, 4, _settings);
            List<BiomeTransition> transitions = Transitions(schedule, 100000.0);

            Assert.Greater(transitions.Count, 30);
            double previous = 0.0;
            foreach (BiomeTransition transition in transitions)
            {
                double length = transition.Distance - previous;
                Assert.GreaterOrEqual(length, _settings.MinSegmentMeters - 1e-6);
                Assert.LessOrEqual(length, _settings.MaxSegmentMeters + 1e-6);
                Assert.AreNotEqual(transition.From, transition.To);
                Assert.AreEqual(transition.From, schedule.BiomeAt(transition.Distance - 1.0));
                Assert.AreEqual(transition.To, schedule.BiomeAt(transition.Distance + 1.0));
                previous = transition.Distance;
            }
        }

        [Test]
        public void EveryBiomeShowsUp()
        {
            var schedule = new BiomeSchedule(3, 4, _settings);
            var seen = new HashSet<int>();
            for (double d = 0.0; d < 100000.0; d += 500.0)
            {
                seen.Add(schedule.BiomeAt(d));
            }

            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, seen);
        }

        [Test]
        public void TheWeights_AddUpToOne_AndFadeSmoothlyAcrossTheBand()
        {
            var schedule = new BiomeSchedule(11, 4, _settings);
            BiomeTransition change = Transitions(schedule, 10000.0)[0];
            double half = schedule.BlendMeters * 0.5;

            float last = 0f;
            for (double d = change.Distance - half - 10.0; d <= change.Distance + half + 10.0; d += 1.0)
            {
                float sum = 0f;
                for (int biome = 0; biome < 4; biome++)
                {
                    float weight = schedule.Weight(biome, d);
                    Assert.That(weight, Is.InRange(0f, 1f));
                    sum += weight;
                }

                Assert.AreEqual(1f, sum, 1e-5f, $"at {d}");
                float incoming = schedule.Weight(change.To, d);
                Assert.GreaterOrEqual(incoming, last - 1e-6f, "The new biome only grows across the band.");
                last = incoming;
            }

            Assert.AreEqual(0.5f, schedule.Weight(change.To, change.Distance), 1e-4f, "Half and half at the middle.");
            Assert.AreEqual(1f, schedule.Weight(change.From, change.Distance - half - 1.0));
            Assert.AreEqual(1f, schedule.Weight(change.To, change.Distance + half + 1.0));
        }

        [Test]
        public void TryTransition_FindsABandThatOnlyTouchesTheRange_AndNothingInTheMiddleOfAStretch()
        {
            var schedule = new BiomeSchedule(5, 4, _settings);
            BiomeTransition change = Transitions(schedule, 10000.0)[0];
            double half = schedule.BlendMeters * 0.5;

            Assert.IsTrue(schedule.TryTransition(change.Distance + half - 1.0, change.Distance + 1000.0, out BiomeTransition behind));
            Assert.AreEqual(change.Distance, behind.Distance);
            Assert.IsTrue(schedule.TryTransition(0.0, change.Distance - half + 1.0, out BiomeTransition ahead));
            Assert.AreEqual(change.Distance, ahead.Distance);
            Assert.IsFalse(schedule.TryTransition(change.Distance + half + 1.0, change.Distance + half + 500.0, out _));
        }

        [Test]
        public void OneBiome_IsEverywhereAtFullWeight()
        {
            var schedule = new BiomeSchedule(9, 1, _settings);

            for (double d = 0.0; d < 20000.0; d += 33.0)
            {
                Assert.AreEqual(0, schedule.BiomeAt(d));
                Assert.AreEqual(1f, schedule.Weight(0, d));
            }
        }

        [Test]
        public void NonsenseSettings_OrNoBiome_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BiomeSchedule(1, 0, _settings));
            Assert.Throws<ArgumentException>(() => new BiomeSchedule(1, 4, new BiomeSettings(100f, 3000f, 150f)), "A stretch shorter than its blend.");
            Assert.Throws<ArgumentException>(() => new BiomeSchedule(1, 4, new BiomeSettings(2000f, 1000f, 150f)), "Max below min.");
            Assert.Throws<ArgumentException>(() => new BiomeSchedule(1, 4, new BiomeSettings(2000f, 3000f, 0f)), "No blend.");
        }
    }
}
