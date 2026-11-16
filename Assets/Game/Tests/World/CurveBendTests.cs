using System;
using NUnit.Framework;
using WrongLine.Driving;

namespace WrongLine.World.Tests
{
    public class CurveBendTests
    {
        [Test]
        public void NothingNearerThanTheStart_SinksAtAll()
        {
            var settings = new CurveWorldSettings { StartMeters = 30f, Strength = 0.001f };

            Assert.AreEqual(0f, CurveBend.Drop(settings, 0f));
            Assert.AreEqual(0f, CurveBend.Drop(settings, 30f));
        }

        [Test]
        public void BeyondTheStart_TheDropGrowsWithTheSquareOfTheExtraDistance()
        {
            var settings = new CurveWorldSettings { StartMeters = 30f, Strength = 0.001f };

            Assert.AreEqual(0.001f * 10f * 10f, CurveBend.Drop(settings, 40f), 1e-5f);
            Assert.AreEqual(0.001f * 20f * 20f, CurveBend.Drop(settings, 50f), 1e-5f);
            Assert.AreEqual(4f * CurveBend.Drop(settings, 40f), CurveBend.Drop(settings, 50f), 1e-5f);
        }

        [Test]
        public void TheDropNeverShrinksWithDistance()
        {
            var settings = new CurveWorldSettings();
            float previous = -1f;

            for (float distance = 0f; distance <= 1000f; distance += 7f)
            {
                float drop = CurveBend.Drop(settings, distance);
                Assert.GreaterOrEqual(drop, previous, $"at {distance} m");
                previous = drop;
            }
        }

        [Test]
        public void ZeroStrength_BendsNothing()
        {
            var settings = new CurveWorldSettings { Strength = 0f };

            Assert.AreEqual(0f, CurveBend.Drop(settings, 5000f));
        }

        [Test]
        public void MaxDrop_CoversEveryPointWithinTheDistance()
        {
            var settings = new CurveWorldSettings();
            float max = CurveBend.MaxDrop(settings, 400f);

            for (float distance = 0f; distance <= 400f; distance += 3f)
            {
                Assert.LessOrEqual(CurveBend.Drop(settings, distance), max + 1e-5f, $"at {distance} m");
            }
        }

        [Test]
        public void TheBend_PullsTheRoadEndFurtherBelowTheHorizonThanAFlatWorld()
        {
            var settings = new CurveWorldSettings();
            var flat = new CurveWorldSettings { Strength = 0f };
            float cameraHeight = new ChaseCameraSettings().Height;

            float bent = CurveBend.AngleBelowHorizon(settings, cameraHeight, 300f);
            float straight = CurveBend.AngleBelowHorizon(flat, cameraHeight, 300f);

            Assert.Greater(bent, straight + 3f, $"bent {bent:F1} degrees, flat {straight:F1} degrees");
        }

        [TestCase(-1f, 0.001f)]
        [TestCase(30f, -0.001f)]
        [TestCase(float.NaN, 0.001f)]
        [TestCase(30f, float.NaN)]
        public void Validate_RejectsNumbersThatMakeNoSense(float start, float strength)
        {
            var settings = new CurveWorldSettings { StartMeters = start, Strength = strength };

            Assert.Throws<ArgumentException>(() => settings.Validate());
        }

        [Test]
        public void Validate_AcceptsTheDefaults()
        {
            Assert.DoesNotThrow(() => new CurveWorldSettings().Validate());
        }
    }
}
