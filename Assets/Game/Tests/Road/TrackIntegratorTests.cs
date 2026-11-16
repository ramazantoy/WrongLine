using System;
using NUnit.Framework;
using UnityEngine;

namespace WrongLine.Road.Tests
{
    public class TrackIntegratorTests
    {
        private const float PositionTolerance = 0.01f;
        private const float AngleTolerance = 0.1f;

        [Test]
        public void Straight_ExitsAtTheLength_AndStaysLevel()
        {
            TrackShape shape = TrackIntegrator.Bake(TrackRecipe.Straight(20f));

            Assert.AreEqual(41, shape.Count);
            Assert.AreEqual(20f, shape.Length, 1e-4f);
            AssertVector(new Vector3(0f, 0f, 20f), shape.Exit.Position, 1e-3f);
            for (int i = 0; i < shape.Count; i++)
            {
                AssertVector(Vector3.up, shape[i].Up, 1e-4f);
                AssertVector(Vector3.forward, shape[i].Forward, 1e-4f);
            }
        }

        [Test]
        public void RightTurn_TurnsTheHeadingAndMatchesAnIndependentIntegration()
        {
            var recipe = new TrackRecipe(40f, 90f, YawProfile.Single, 0f, PitchProfile.Bump, 0f);

            TrackShape shape = TrackIntegrator.Bake(recipe);

            AssertVector(Vector3.right, shape.Exit.Forward, 1e-3f);
            AssertVector(ReferenceExit(recipe), shape.Exit.Position, PositionTolerance);
            Assert.Greater(shape.Exit.Position.x, 5f, "A right turn ends to the right of the entry line.");
            Assert.Less(shape.Exit.Position.z, 40f, "The chord of a bend is shorter than the piece.");
        }

        [Test]
        public void LeftTurn_IsTheMirrorOfTheRightTurn()
        {
            var right = new TrackRecipe(40f, 60f, YawProfile.Single, 0f, PitchProfile.Bump, 0f);
            var left = new TrackRecipe(40f, -60f, YawProfile.Single, 0f, PitchProfile.Bump, 0f);

            TrackShape a = TrackIntegrator.Bake(right);
            TrackShape b = TrackIntegrator.Bake(left);

            AssertVector(new Vector3(-a.Exit.Position.x, a.Exit.Position.y, a.Exit.Position.z), b.Exit.Position, 1e-3f);
        }

        [Test]
        public void Ramp_ClimbsAndLeavesLevel()
        {
            var recipe = new TrackRecipe(40f, 0f, YawProfile.Single, 10f, PitchProfile.Bump, 0f);

            TrackShape shape = TrackIntegrator.Bake(recipe);

            Assert.Greater(shape.Exit.Position.y, 1f);
            Assert.AreEqual(0f, shape.Exit.Forward.y, 1e-3f, "The ramp ends level.");
            Assert.AreEqual(Mathf.Sin(10f * Mathf.Deg2Rad), shape[shape.Count / 2].Forward.y, 1e-3f, "The slope peaks mid-piece.");
            AssertVector(ReferenceExit(recipe), shape.Exit.Position, PositionTolerance);
        }

        [Test]
        public void Dip_DropsBelowTheEntry()
        {
            var recipe = new TrackRecipe(40f, 0f, YawProfile.Single, -10f, PitchProfile.Bump, 0f);

            Assert.Less(TrackIntegrator.Bake(recipe).Exit.Position.y, -1f);
        }

        [Test]
        public void Hill_RisesThenReturnsToTheEntryHeight()
        {
            var recipe = new TrackRecipe(60f, 0f, YawProfile.Single, 12f, PitchProfile.Wave, 0f);

            TrackShape shape = TrackIntegrator.Bake(recipe);

            Assert.AreEqual(0f, shape.Exit.Position.y, 1e-3f);
            float highest = 0f;
            for (int i = 0; i < shape.Count; i++)
            {
                highest = Mathf.Max(highest, shape[i].Position.y);
            }

            Assert.Greater(highest, 0.5f);
        }

        [Test]
        public void SCurve_LeavesStraightButOffset()
        {
            var recipe = new TrackRecipe(60f, 40f, YawProfile.S, 0f, PitchProfile.Bump, 0f);

            TrackShape shape = TrackIntegrator.Bake(recipe);

            AssertVector(Vector3.forward, shape.Exit.Forward, 1e-3f);
            Assert.Greater(shape.Exit.Position.x, 1f, "The first half turns right, so the exit is offset to the right.");
            AssertVector(ReferenceExit(recipe), shape.Exit.Position, PositionTolerance);
        }

        [Test]
        public void Bank_RollsTheRoadIntoTheTurn_AndLevelsOutAtTheEnds()
        {
            var recipe = new TrackRecipe(40f, 60f, YawProfile.Single, 0f, PitchProfile.Bump, 20f);

            TrackShape shape = TrackIntegrator.Bake(recipe);

            TrackFrame middle = shape[shape.Count / 2];
            Assert.AreEqual(20f, Vector3.Angle(Vector3.up, middle.Up), AngleTolerance, "Full bank at the tightest point.");
            Assert.Less(middle.Right.y, -0.3f, "Positive bank lowers the right edge (sin 20 deg = 0.34).");
            AssertVector(Vector3.up, shape.Entry.Up, 1e-4f);
            AssertVector(Vector3.up, shape.Exit.Up, 1e-4f);
        }

        [TestCase(40f, 90f, YawProfile.Single, 0f, PitchProfile.Bump, 0f)]
        [TestCase(40f, -90f, YawProfile.Single, 0f, PitchProfile.Bump, 25f)]
        [TestCase(60f, 45f, YawProfile.S, 0f, PitchProfile.Bump, 15f)]
        [TestCase(60f, 0f, YawProfile.Single, 15f, PitchProfile.Bump, 0f)]
        [TestCase(60f, 0f, YawProfile.Single, 15f, PitchProfile.Wave, 0f)]
        [TestCase(80f, 120f, YawProfile.Single, 10f, PitchProfile.Wave, 30f)]
        public void EveryPiece_StartsAndEndsFlatLevelAndStraight(float length, float yaw, YawProfile yawProfile, float pitch, PitchProfile pitchProfile, float bank)
        {
            TrackShape shape = TrackIntegrator.Bake(new TrackRecipe(length, yaw, yawProfile, pitch, pitchProfile, bank));

            AssertFlatEnd(shape.Entry, shape[1]);
            AssertFlatEnd(shape.Exit, shape[shape.Count - 2]);
        }

        [Test]
        public void FramesAreOneStepApart()
        {
            TrackShape shape = TrackIntegrator.Bake(new TrackRecipe(60f, 80f, YawProfile.Single, 10f, PitchProfile.Bump, 10f));

            for (int i = 1; i < shape.Count; i++)
            {
                float distance = Vector3.Distance(shape[i - 1].Position, shape[i].Position);
                Assert.AreEqual(TrackIntegrator.Step, distance, 0.005f, $"Frames {i - 1} and {i}.");
            }
        }

        [Test]
        public void Evaluate_InterpolatesBetweenFrames_AndClampsAtTheEnds()
        {
            TrackShape shape = TrackIntegrator.Bake(TrackRecipe.Straight(10f));

            AssertVector(new Vector3(0f, 0f, 0.25f), shape.Evaluate(0.25f).Position, 1e-4f);
            AssertVector(new Vector3(0f, 0f, 7.3f), shape.Evaluate(7.3f).Position, 1e-4f);
            AssertVector(shape.Entry.Position, shape.Evaluate(-5f).Position, 1e-6f);
            AssertVector(shape.Exit.Position, shape.Evaluate(10f).Position, 1e-6f);
            AssertVector(shape.Exit.Position, shape.Evaluate(99f).Position, 1e-6f);
        }

        [TestCase(0f)]
        [TestCase(-4f)]
        [TestCase(10.3f)]
        public void LengthThatIsNotAWholeNumberOfSteps_IsRejected(float length)
        {
            Assert.Throws<ArgumentException>(() => TrackIntegrator.Bake(TrackRecipe.Straight(length)));
        }

        // The heading must be nearly constant across the first interval: the bend opens with zero curvature.
        private static void AssertFlatEnd(TrackFrame end, TrackFrame neighbour)
        {
            AssertVector(Vector3.up, end.Up, 1e-4f);
            Assert.AreEqual(0f, end.Forward.y, 1e-4f, "Level.");
            Assert.Less(Vector3.Angle(end.Forward, neighbour.Forward), 0.1f, "Straight: no curvature at the joint.");
            Assert.Less(Vector3.Angle(end.Up, neighbour.Up), 0.1f, "No roll rate at the joint.");
        }

        private static void AssertVector(Vector3 expected, Vector3 actual, float tolerance)
        {
            Assert.AreEqual(0f, Vector3.Distance(expected, actual), tolerance, $"Expected {expected}, was {actual}.");
        }

        // Independent of the closed forms in TrackProfiles: integrates the curvature and slope profiles numerically.
        private static Vector3 ReferenceExit(in TrackRecipe recipe)
        {
            const int Steps = 40000;
            double yawTotal = recipe.YawDegrees * Math.PI / 180.0;
            double pitchPeak = recipe.PitchDegrees * Math.PI / 180.0;
            double dx = recipe.Length / Steps;
            double x = 0.0;
            double y = 0.0;
            double z = 0.0;
            double yaw = 0.0;

            for (int i = 0; i < Steps; i++)
            {
                double u = (i + 0.5) / Steps;
                yaw += yawTotal * YawRate(recipe.YawProfile, u) / Steps;
                double sin = Math.Sin(Math.PI * u);
                double pitch = pitchPeak * (recipe.PitchProfile == PitchProfile.Bump ? sin * sin : sin * sin * Math.Sin(2.0 * Math.PI * u));
                x += Math.Cos(pitch) * Math.Sin(yaw) * dx;
                y += Math.Sin(pitch) * dx;
                z += Math.Cos(pitch) * Math.Cos(yaw) * dx;
            }

            return new Vector3((float)x, (float)y, (float)z);
        }

        private static double YawRate(YawProfile profile, double u)
        {
            if (profile == YawProfile.Single)
            {
                double s = Math.Sin(Math.PI * u);
                return 2.0 * s * s;
            }

            double t = Math.Sin(2.0 * Math.PI * u);
            return u <= 0.5 ? 4.0 * t * t : -4.0 * t * t;
        }
    }
}
