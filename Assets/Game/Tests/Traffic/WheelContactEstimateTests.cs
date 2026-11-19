using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace WrongLine.Traffic.Tests
{
    public class WheelContactEstimateTests
    {
        /// <summary>A car's worth of vertices: a body well above the road, and four tyre patches at the corners (a few points each).</summary>
        private static List<Vector3> Car(float frontAxle, float rearAxle, float halfTrack)
        {
            var vertices = new List<Vector3>
            {
                new Vector3(-0.9f, 0.5f, 2f), new Vector3(0.9f, 0.5f, -2f), new Vector3(0f, 1.4f, 0f), // Body: high.
            };
            foreach (float z in new[] { frontAxle, rearAxle })
            {
                foreach (float x in new[] { -halfTrack, halfTrack })
                {
                    float centre = x;
                    vertices.Add(new Vector3(centre, 0f, z - 0.12f));
                    vertices.Add(new Vector3(centre, 0f, z + 0.12f));
                    vertices.Add(new Vector3(centre, 0.02f, z));
                    vertices.Add(new Vector3(centre, 0.3f, z)); // Up the tyre: not the contact.
                }
            }

            return vertices;
        }

        [Test]
        public void TheAxlesAndTheTrack_AreTheMiddlesOfTheTyrePatches()
        {
            Assert.IsTrue(WheelContactEstimate.TryEstimate(Car(1.25f, -1.35f, 0.82f), 0.05f, out WheelContactEstimate.Result result));

            Assert.AreEqual(1.25f, result.FrontAxle, 1e-4f);
            Assert.AreEqual(-1.35f, result.RearAxle, 1e-4f);
            Assert.AreEqual(0.82f, result.HalfTrack, 1e-4f);
        }

        [Test]
        public void ABodyAboveTheBand_IsNotTakenForAWheel()
        {
            List<Vector3> vertices = Car(1.2f, -1.2f, 0.8f);
            vertices.Add(new Vector3(0f, 0.06f, 2.2f)); // A bumper lip just above the band.

            WheelContactEstimate.TryEstimate(vertices, 0.05f, out WheelContactEstimate.Result result);

            Assert.AreEqual(1.2f, result.FrontAxle, 1e-4f);
        }

        [Test]
        public void ACarWithoutFourCorners_IsRefused()
        {
            var oneSide = new List<Vector3>();
            foreach (Vector3 v in Car(1.2f, -1.2f, 0.8f))
            {
                if (v.x < 0f || v.y > 0.05f)
                {
                    oneSide.Add(v);
                }
            }

            Assert.IsFalse(WheelContactEstimate.TryEstimate(oneSide, 0.05f, out _));
            Assert.IsFalse(WheelContactEstimate.TryEstimate(new List<Vector3> { new Vector3(0f, 1f, 0f) }, 0.05f, out _), "Nothing on the ground.");
        }

        [Test]
        public void AFrontLeftWheelFurtherForwardThanTheRight_StillGivesTheMiddleOfThePair()
        {
            List<Vector3> vertices = Car(1.2f, -1.2f, 0.8f);
            vertices.Add(new Vector3(-0.8f, 0f, 1.4f));

            WheelContactEstimate.TryEstimate(vertices, 0.05f, out WheelContactEstimate.Result result);

            Assert.Greater(result.FrontAxle, 1.2f);
            Assert.Less(result.FrontAxle, 1.4f);
        }
    }
}
