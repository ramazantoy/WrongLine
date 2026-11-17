using System;
using NUnit.Framework;
using UnityEngine;
using WrongLine.World;

namespace WrongLine.Scenery.Tests
{
    public class LampPickerTests
    {
        private static Vector3[] Lamps(params float[] xs)
        {
            var lamps = new Vector3[xs.Length];
            for (int i = 0; i < xs.Length; i++)
            {
                lamps[i] = new Vector3(xs[i], 5f, 0f);
            }

            return lamps;
        }

        [Test]
        public void ItPicksTheNearestLamps_NearestFirst()
        {
            var picker = new LampPicker();
            var result = new Vector3[LampPoolFalloff.MaxPools];

            int count = picker.Pick(Lamps(40f, 10f, 30f, 5f, 20f), Vector3.zero, 3, result);

            Assert.AreEqual(3, count);
            Assert.AreEqual(5f, result[0].x);
            Assert.AreEqual(10f, result[1].x);
            Assert.AreEqual(20f, result[2].x);
        }

        [Test]
        public void WithFewerLampsThanAsked_ItGivesThemAll()
        {
            var picker = new LampPicker();
            var result = new Vector3[LampPoolFalloff.MaxPools];

            Assert.AreEqual(2, picker.Pick(Lamps(9f, 3f), Vector3.zero, 6, result));
            Assert.AreEqual(3f, result[0].x);
            Assert.AreEqual(0, picker.Pick(Lamps(), Vector3.zero, 6, result));
            Assert.AreEqual(0, picker.Pick(Lamps(1f, 2f), Vector3.zero, 0, result));
        }

        [Test]
        public void TheDistanceIsAcrossTheGround_Height_DoesNotCount()
        {
            var picker = new LampPicker();
            var result = new Vector3[LampPoolFalloff.MaxPools];
            var high = new Vector3(10f, 500f, 0f);
            var low = new Vector3(12f, -3f, 0f);

            picker.Pick(new[] { low, high }, Vector3.zero, 1, result);

            Assert.AreEqual(high, result[0]);
        }

        [Test]
        public void OnATie_TheEarlierLampComesFirst_AndTheOrderIsStable()
        {
            var picker = new LampPicker();
            var result = new Vector3[LampPoolFalloff.MaxPools];
            var a = new Vector3(10f, 1f, 0f);
            var b = new Vector3(-10f, 2f, 0f);
            var c = new Vector3(0f, 3f, 10f);

            picker.Pick(new[] { a, b, c }, Vector3.zero, 2, result);

            Assert.AreEqual(a, result[0]);
            Assert.AreEqual(b, result[1]);
        }

        [Test]
        public void TheResultIsRelativeToTheCamera()
        {
            var picker = new LampPicker();
            var result = new Vector3[LampPoolFalloff.MaxPools];

            picker.Pick(Lamps(0f, 100f), new Vector3(95f, 0f, 0f), 1, result);

            Assert.AreEqual(100f, result[0].x);
        }

        [TestCase(-1)]
        [TestCase(LampPoolFalloff.MaxPools + 1)]
        public void AskingForMoreThanTheShaderTakes_Throws(int max)
        {
            var picker = new LampPicker();

            Assert.Throws<ArgumentOutOfRangeException>(() => picker.Pick(Lamps(1f), Vector3.zero, max, new Vector3[16]));
        }
    }
}
