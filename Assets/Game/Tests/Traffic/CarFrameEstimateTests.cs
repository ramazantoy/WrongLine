using NUnit.Framework;
using UnityEngine;

namespace WrongLine.Traffic.Tests
{
    public class CarFrameEstimateTests
    {
        // A car in its own frame: 1.8 wide (x), 1.5 tall (y), 4.5 long (z), nose at +z; headlights at the nose, brake lights at the tail,
        // tyres low (y 0.3) and level across.
        private static readonly Vector3 Size = new Vector3(1.8f, 1.5f, 4.5f);
        private static readonly Vector3 Centre = new Vector3(0f, 0.75f, 0f);
        private static readonly Vector3 Head = new Vector3(0f, 0.7f, 2.2f);
        private static readonly Vector3 Stop = new Vector3(0f, 0.8f, -2.2f);
        private static readonly Vector3 Dark = new Vector3(0f, 0.3f, 0f);

        /// <summary>The same car as a file might hold it: <paramref name="toFile"/> turns the car's own axes into the file's.</summary>
        private static bool Estimate(Quaternion toFile, out Vector3 right, out Vector3 up, out Vector3 forward, out Matrix4x4 m)
        {
            m = Matrix4x4.Rotate(toFile);
            Vector3 size = Abs(m.MultiplyVector(Size));
            return CarFrameEstimate.TryEstimate(m.MultiplyPoint(Centre), size, m.MultiplyPoint(Head), m.MultiplyPoint(Stop), m.MultiplyPoint(Dark), out right, out up, out forward);
        }

        private static Vector3 Abs(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        [Test]
        public void TheCarInItsOwnFrame_GivesTheUnitAxes()
        {
            Assert.IsTrue(Estimate(Quaternion.identity, out Vector3 right, out Vector3 up, out Vector3 forward, out _));

            Assert.AreEqual(0f, Vector3.Distance(Vector3.forward, forward), 1e-5f);
            Assert.AreEqual(0f, Vector3.Distance(Vector3.up, up), 1e-5f);
            Assert.AreEqual(0f, Vector3.Distance(Vector3.right, right), 1e-5f);
        }

        [Test]
        public void TheCarAnyWayRound_ComesBackAsItsOwnAxes()
        {
            // Every orientation a file could hold it in: the four turns about the vertical, stood on its nose or its side, Z-up, upside down.
            foreach (Quaternion toFile in Orientations())
            {
                Assert.IsTrue(Estimate(toFile, out Vector3 right, out Vector3 up, out Vector3 forward, out Matrix4x4 m), $"{toFile.eulerAngles}");

                Assert.AreEqual(0f, Vector3.Distance(m.MultiplyVector(Vector3.forward), forward), 1e-4f, $"forward {toFile.eulerAngles}");
                Assert.AreEqual(0f, Vector3.Distance(m.MultiplyVector(Vector3.up), up), 1e-4f, $"up {toFile.eulerAngles}");
                Assert.AreEqual(0f, Vector3.Distance(m.MultiplyVector(Vector3.right), right), 1e-4f, $"right {toFile.eulerAngles}");
            }
        }

        private static Quaternion[] Orientations()
        {
            var list = new System.Collections.Generic.List<Quaternion>();
            foreach (float yaw in new[] { 0f, 90f, 180f, 270f })
            {
                foreach (float pitch in new[] { 0f, 90f, 180f, 270f })
                {
                    list.Add(Quaternion.Euler(pitch, yaw, 0f));
                    list.Add(Quaternion.Euler(0f, yaw, pitch));
                }
            }

            return list.ToArray();
        }

        [Test]
        public void ATallVehicle_IsNotTakenForAZUpCar()
        {
            // A box truck: 2.3 wide, 3.0 tall, 7.5 long. Taller than wide, but the length is still the long axis and the tyres still low.
            var size = new Vector3(2.3f, 3f, 7.5f);
            Assert.IsTrue(CarFrameEstimate.TryEstimate(new Vector3(0f, 1.5f, 0f), size, new Vector3(0f, 1f, 3.6f), new Vector3(0f, 1f, -3.7f), new Vector3(0f, 0.4f, 0.5f), out Vector3 right, out Vector3 up, out Vector3 forward));

            Assert.AreEqual(Vector3.forward, forward);
            Assert.AreEqual(Vector3.up, up);
            Assert.AreEqual(Vector3.right, right);
        }

        [Test]
        public void LightsNotToldApart_OrTyresNotLow_AreRefused()
        {
            Assert.IsFalse(CarFrameEstimate.TryEstimate(Centre, Size, new Vector3(0f, 0.7f, 0.1f), new Vector3(0f, 0.8f, -0.1f), Dark, out _, out _, out _), "Both lights in the middle.");
            Assert.IsFalse(CarFrameEstimate.TryEstimate(Centre, Size, Head, Stop, Centre, out _, out _, out _), "Dark parts as high as the middle.");
            Assert.IsFalse(CarFrameEstimate.TryEstimate(Centre, Vector3.zero, Head, Stop, Dark, out _, out _, out _), "No size.");
        }
    }
}
