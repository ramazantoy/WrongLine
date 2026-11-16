using System;
using AF.Core.Editor;
using NUnit.Framework;
using UnityEngine;
using WrongLine.World.Editor;

namespace WrongLine.World.Tests
{
    public class FogRangeTests
    {
        [Test]
        public void TheFog_IsThickBeforeTheEndOfTheRoadBuiltAhead()
        {
            var settings = new CurveWorldSettings { FogStartFraction = 0.25f, FogEndMargin = 40f };

            (float start, float end) = FogRange.Compute(settings, 300f);

            Assert.AreEqual(75f, start, 1e-4f);
            Assert.AreEqual(260f, end, 1e-4f);
            Assert.Less(end, 300f);
        }

        [TestCase(100f)]
        [TestCase(300f)]
        [TestCase(1000f)]
        public void TheFog_NeverEndsAfterTheRoadDoes_AndStartsBeforeItEnds(float ahead)
        {
            var settings = new CurveWorldSettings();

            (float start, float end) = FogRange.Compute(settings, ahead);

            Assert.LessOrEqual(end, ahead - settings.FogEndMargin + 1e-4f);
            Assert.Less(start, end);
            Assert.GreaterOrEqual(start, 0f);
        }

        [TestCase(300f, 0.25f, 260f)]
        [TestCase(300f, 0.25f, 300f)]
        [TestCase(300f, 0.95f, 40f)]
        [TestCase(0f, 0.25f, 0f)]
        [TestCase(-50f, 0.25f, 0f)]
        public void AFogThatCannotFit_IsRejected(float ahead, float startFraction, float margin)
        {
            var settings = new CurveWorldSettings { FogStartFraction = startFraction, FogEndMargin = margin };

            Assert.Throws<ArgumentException>(() => FogRange.Compute(settings, ahead));
        }

        [TestCase(-0.1f, 40f)]
        [TestCase(1f, 40f)]
        [TestCase(0.25f, -1f)]
        [TestCase(float.NaN, 40f)]
        public void Validate_RejectsFogNumbersThatMakeNoSense(float fraction, float margin)
        {
            var settings = new CurveWorldSettings { FogStartFraction = fraction, FogEndMargin = margin };

            Assert.Throws<ArgumentException>(() => settings.Validate());
        }

        [Test]
        public void TheValidator_FlagsAFogThatDoesNotFitTheRoad()
        {
            var config = ScriptableObject.CreateInstance<CurveWorldConfig>();
            try
            {
                var report = new ValidationReport();

                CurveWorldConfigValidator.CheckFog(config, 30f, "test", report);

                Assert.Greater(report.ErrorCount, 0, "30 m of road cannot hold a 40 m fog margin");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void TheValidator_AcceptsTheDefaultsWithTheDefaultRoad()
        {
            var config = ScriptableObject.CreateInstance<CurveWorldConfig>();
            try
            {
                var report = new ValidationReport();

                CurveWorldConfigValidator.Check(config, "test", report);
                CurveWorldConfigValidator.CheckFog(config, 300f, "test", report);

                Assert.AreEqual(0, report.ErrorCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
