using System;
using NUnit.Framework;
using UnityEngine;
using WrongLine.Road.Editor;

namespace WrongLine.Road.Tests
{
    public class RoadPoseTests
    {
        private const double Tolerance = 1e-9;

        [Test]
        public void FacingForward_PointsMapStraightThrough()
        {
            WorldPoint point = RoadPose.Origin.TransformPoint(new Vector3(2f, 3f, 10f));

            AssertPoint(2.0, 3.0, 10.0, point);
        }

        [Test]
        public void TurnedRight90_ForwardBecomesPlusX_AndRightBecomesMinusZ()
        {
            var pose = new RoadPose(5.0, 1.0, 7.0, Math.PI / 2.0);

            AssertPoint(15.0, 1.0, 7.0, pose.TransformPoint(new Vector3(0f, 0f, 10f)));
            AssertPoint(5.0, 1.0, 6.0, pose.TransformPoint(new Vector3(1f, 0f, 0f)));
        }

        [Test]
        public void Directions_RotateTheSameWay_WithoutTranslating()
        {
            var pose = new RoadPose(100.0, 50.0, 100.0, Math.PI / 2.0);

            Vector3 forward = pose.TransformDirection(Vector3.forward);

            Assert.AreEqual(0f, Vector3.Distance(Vector3.right, forward), 1e-6f);
            Assert.AreEqual(Vector3.up, pose.TransformDirection(Vector3.up));
        }

        [Test]
        public void After_ComposesTheExitOffsetAndTheTurn()
        {
            var right = new RoadPieceInfo(1, "Right", new TrackRecipe(40f, 90f, YawProfile.Single, 0f, PitchProfile.Bump, 0f), false);

            RoadPose exit = RoadPose.Origin.After(right);

            Assert.AreEqual(right.ExitOffset.x, exit.X, 1e-6);
            Assert.AreEqual(right.ExitOffset.z, exit.Z, 1e-6);
            Assert.AreEqual(90.0, exit.YawDegrees, 1e-9);

            // A second identical piece, entered facing right, must continue towards +X and then turn to face backwards (-Z).
            RoadPose second = exit.After(right);
            Assert.AreEqual(exit.X + right.ExitOffset.z, second.X, 1e-5, "Its forward offset runs along +X.");
            Assert.AreEqual(exit.Z - right.ExitOffset.x, second.Z, 1e-5, "Its sideways offset turns into -Z.");
            Assert.AreEqual(180.0, second.YawDegrees, 1e-9);
        }

        [Test]
        public void ExitYaw_MatchesTheBakedExitHeading()
        {
            foreach (RoadPieceSpecs.Spec spec in RoadPieceSpecs.Defaults)
            {
                var info = new RoadPieceInfo(0, spec.Name, spec.Recipe, spec.IsStart);
                Vector3 forward = info.Shape.Exit.Forward;

                Assert.AreEqual(Math.Atan2(forward.x, forward.z), info.ExitYaw, 1e-4, spec.Name);
            }
        }

        private static void AssertPoint(double x, double y, double z, WorldPoint actual)
        {
            Assert.AreEqual(x, actual.X, Tolerance);
            Assert.AreEqual(y, actual.Y, Tolerance);
            Assert.AreEqual(z, actual.Z, Tolerance);
        }
    }
}
