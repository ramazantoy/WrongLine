using System;
using NUnit.Framework;
using UnityEngine;

namespace WrongLine.World.Tests
{
    public class DayNightRuleTests
    {
        private static Gradient TwoColors(Color from, Color to)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        private static DayNightSettings Simple()
        {
            return new DayNightSettings
            {
                CycleSeconds = 100f,
                StartPhase = 0f,
                Zenith = TwoColors(Color.black, Color.white),
                Horizon = TwoColors(Color.red, Color.blue),
                Ground = TwoColors(Color.black, Color.black),
                Light = TwoColors(Color.white, Color.white),
                LightIntensity = AnimationCurve.Linear(0f, 0f, 1f, 1f),
            };
        }

        private static void AssertColorsClose(Color expected, Color actual, float tolerance, string message)
        {
            Assert.AreEqual(expected.r, actual.r, tolerance, message + " (r)");
            Assert.AreEqual(expected.g, actual.g, tolerance, message + " (g)");
            Assert.AreEqual(expected.b, actual.b, tolerance, message + " (b)");
        }

        [Test]
        public void StartPhaseFor_GivesTheSameSkyForTheSameSeed()
        {
            var rule = new DayNightRule(Simple());

            Assert.AreEqual(rule.StartPhaseFor(12345UL), rule.StartPhaseFor(12345UL));
            Assert.AreNotEqual(rule.StartPhaseFor(12345UL), rule.StartPhaseFor(12346UL));
        }

        [Test]
        public void StartPhaseFor_ReachesEveryPartOfTheDay_AcrossSeeds()
        {
            var rule = new DayNightRule(Simple());
            var quarters = new int[4];

            for (ulong seed = 0; seed < 200; seed++)
            {
                float phase = rule.StartPhaseFor(seed);
                Assert.That(phase, Is.InRange(0f, 1f));
                quarters[Mathf.Min(3, (int)(phase * 4f))]++;
            }

            foreach (int count in quarters)
            {
                Assert.Greater(count, 20, $"quarters: {string.Join(", ", quarters)}");
            }
        }

        [Test]
        public void StartPhaseFor_StaysBetweenTheConfiguredPhases()
        {
            var settings = Simple();
            settings.StartPhaseMin = 0.6f;
            settings.StartPhaseMax = 0.8f;
            var rule = new DayNightRule(settings);

            for (ulong seed = 0; seed < 200; seed++)
            {
                Assert.That(rule.StartPhaseFor(seed), Is.InRange(0.6f, 0.8f));
            }
        }

        [Test]
        public void StartPhaseFor_WithTheRandomStartOff_IsTheConfiguredStartPhase()
        {
            var settings = Simple();
            settings.RandomStart = false;
            settings.StartPhase = 0.37f;
            var rule = new DayNightRule(settings);

            Assert.AreEqual(0.37f, rule.StartPhaseFor(1UL));
            Assert.AreEqual(0.37f, rule.StartPhaseFor(999UL));
        }

        [Test]
        public void Validate_RejectsStartPhasesThatCannotBeDrawnFrom()
        {
            var settings = Simple();
            settings.StartPhaseMin = 0.7f;
            settings.StartPhaseMax = 0.3f;

            Assert.Throws<ArgumentException>(() => new DayNightRule(settings));
        }

        [Test]
        public void PhaseAt_WithAStartPhase_RunsFromThatPhase()
        {
            var rule = new DayNightRule(Simple());

            Assert.AreEqual(0.8f, rule.PhaseAt(0f, 0.8f), 1e-5f);
            Assert.AreEqual(0.1f, rule.PhaseAt(30f, 0.8f), 1e-5f, "100 s per cycle: 30 s on from 0.8 wraps to 0.1.");
        }

        [Test]
        public void PhaseAt_StartsAtTheStartPhaseAndRunsOneCycleInCycleSeconds()
        {
            var settings = Simple();
            settings.StartPhase = 0.25f;
            var rule = new DayNightRule(settings);

            Assert.AreEqual(0.25f, rule.PhaseAt(0f), 1e-5f);
            Assert.AreEqual(0.5f, rule.PhaseAt(25f), 1e-5f);
            Assert.AreEqual(0.25f, rule.PhaseAt(100f), 1e-4f);
            Assert.AreEqual(0.25f, rule.PhaseAt(1000f), 1e-3f);
        }

        [TestCase(0f)]
        [TestCase(0.37f)]
        [TestCase(0.999f)]
        [TestCase(12345f)]
        public void PhaseAt_StaysInsideZeroToOne(float seconds)
        {
            float phase = new DayNightRule(Simple()).PhaseAt(seconds);

            Assert.GreaterOrEqual(phase, 0f);
            Assert.Less(phase, 1f);
        }

        [Test]
        public void Evaluate_BetweenKeys_BlendsTheColours()
        {
            var rule = new DayNightRule(Simple());

            SkyState half = rule.Evaluate(0.5f);

            AssertColorsClose(new Color(0.5f, 0.5f, 0.5f), half.Zenith, 0.02f, "zenith");
            AssertColorsClose(new Color(0.5f, 0f, 0.5f), half.Horizon, 0.02f, "horizon");
        }

        [Test]
        public void TheFog_IsAlwaysTheHorizonColour()
        {
            var rule = new DayNightRule(new DayNightSettings());

            for (float phase = 0f; phase < 1f; phase += 0.037f)
            {
                SkyState sky = rule.Evaluate(phase);
                Assert.AreEqual(sky.Horizon, sky.Fog, $"at phase {phase}");
            }
        }

        [Test]
        public void TheDefaultCycle_ContinuesWithoutAJumpAcrossTheWrap()
        {
            var rule = new DayNightRule(new DayNightSettings());

            SkyState before = rule.Evaluate(0.9999f);
            SkyState after = rule.Evaluate(0.0001f);

            AssertColorsClose(before.Zenith, after.Zenith, 0.01f, "zenith");
            AssertColorsClose(before.Horizon, after.Horizon, 0.01f, "horizon");
            AssertColorsClose(before.Ground, after.Ground, 0.01f, "ground");
            AssertColorsClose(before.LightColor, after.LightColor, 0.01f, "light colour");
            Assert.AreEqual(before.LightIntensity, after.LightIntensity, 0.01f, "intensity");
            Assert.AreEqual(before.LightPitch, after.LightPitch, 0.5f, "pitch");
        }

        [Test]
        public void TheDefaultCycle_IsMuchDarkerAtMidnightThanAtNoon()
        {
            var rule = new DayNightRule(new DayNightSettings());

            SkyState noon = rule.Evaluate(0.25f);
            SkyState midnight = rule.Evaluate(0.75f);

            Assert.Less(midnight.LightIntensity, noon.LightIntensity * 0.5f);
            Assert.Less(midnight.Zenith.grayscale, noon.Zenith.grayscale * 0.3f);
            Assert.Less(midnight.AmbientSky.grayscale, noon.AmbientSky.grayscale * 0.3f);
        }

        [Test]
        public void TheLight_AlwaysShinesDown_BetweenTheConfiguredPitches()
        {
            var settings = new DayNightSettings { MinLightPitch = 20f, MaxLightPitch = 65f };
            var rule = new DayNightRule(settings);

            for (float phase = 0f; phase <= 1f; phase += 0.01f)
            {
                SkyState sky = rule.Evaluate(phase);
                Assert.GreaterOrEqual(sky.LightPitch, 20f - 1e-3f, $"at phase {phase}");
                Assert.LessOrEqual(sky.LightPitch, 65f + 1e-3f, $"at phase {phase}");
                Assert.Less((sky.LightRotation * Vector3.forward).y, 0f, $"at phase {phase}");
            }
        }

        [Test]
        public void TheLight_IsHighestAtNoonAndLowestAtTheHorizons()
        {
            var rule = new DayNightRule(new DayNightSettings());

            Assert.AreEqual(70f, rule.Evaluate(0.25f).LightPitch, 0.01f);
            Assert.AreEqual(15f, rule.Evaluate(0f).LightPitch, 0.01f);
            Assert.AreEqual(15f, rule.Evaluate(0.5f).LightPitch, 0.01f);
        }

        [Test]
        public void TheAmbientColours_AreTheSkyColoursScaledByTheIntensity()
        {
            var settings = Simple();
            settings.AmbientIntensity = 0.5f;
            var rule = new DayNightRule(settings);

            SkyState sky = rule.Evaluate(0.5f);

            AssertColorsClose(new Color(sky.Zenith.r * 0.5f, sky.Zenith.g * 0.5f, sky.Zenith.b * 0.5f), sky.AmbientSky, 1e-5f, "sky");
            AssertColorsClose(new Color(sky.Horizon.r * 0.5f, sky.Horizon.g * 0.5f, sky.Horizon.b * 0.5f), sky.AmbientEquator, 1e-5f, "equator");
        }

        [Test]
        public void Evaluate_WrapsAPhaseOutsideZeroToOne()
        {
            var rule = new DayNightRule(Simple());

            AssertColorsClose(rule.Evaluate(0.3f).Zenith, rule.Evaluate(1.3f).Zenith, 1e-4f, "1.3");
            AssertColorsClose(rule.Evaluate(0.3f).Zenith, rule.Evaluate(-0.7f).Zenith, 1e-4f, "-0.7");
        }

        [TestCase(0f, 0.5f, 0f, 70f)]
        [TestCase(-5f, 0.5f, 0f, 70f)]
        [TestCase(100f, 1.5f, 0f, 70f)]
        [TestCase(100f, 0.5f, 0f, 120f)]
        [TestCase(100f, 0.5f, 80f, 70f)]
        [TestCase(100f, 0.5f, -1f, 70f)]
        public void Validate_RejectsNumbersThatMakeNoSense(float cycle, float start, float minPitch, float maxPitch)
        {
            var settings = new DayNightSettings { CycleSeconds = cycle, StartPhase = start, MinLightPitch = minPitch, MaxLightPitch = maxPitch };

            Assert.Throws<ArgumentException>(() => new DayNightRule(settings));
        }

        [Test]
        public void TheDefaultDarkness_IsFullAtMidnight_AndZeroAtNoonAndSunrise()
        {
            var rule = new DayNightRule(new DayNightSettings());

            Assert.AreEqual(1f, rule.Evaluate(0.75f).Darkness, 1e-3f);
            Assert.AreEqual(0f, rule.Evaluate(0.25f).Darkness, 1e-3f);
            Assert.AreEqual(0f, rule.Evaluate(0f).Darkness, 1e-3f);
        }

        [Test]
        public void TheDefaultDarkness_RisesAfterSunsetAndStaysInsideZeroToOne()
        {
            var rule = new DayNightRule(new DayNightSettings());

            Assert.Less(rule.Evaluate(0.5f).Darkness, rule.Evaluate(0.58f).Darkness);
            for (float phase = 0f; phase <= 1f; phase += 0.01f)
            {
                Assert.That(rule.Evaluate(phase).Darkness, Is.InRange(0f, 1f), $"at phase {phase}");
            }
        }

        [Test]
        public void Validate_RejectsADarknessThatLeavesZeroToOne()
        {
            var settings = Simple();
            settings.Darkness = AnimationCurve.Linear(0f, 0f, 1f, 2f);

            Assert.Throws<ArgumentException>(() => new DayNightRule(settings));
        }

        [Test]
        public void Validate_AcceptsTheDefaults()
        {
            Assert.DoesNotThrow(() => new DayNightRule(new DayNightSettings()));
        }
    }
}
