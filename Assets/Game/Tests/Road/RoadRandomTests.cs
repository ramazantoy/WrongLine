using NUnit.Framework;

namespace WrongLine.Road.Tests
{
    public class RoadRandomTests
    {
        [Test]
        public void MatchesTheReferenceSplitMix64Sequence()
        {
            // Values from an independent implementation of the published algorithm.
            var random = new RoadRandom(0);

            Assert.AreEqual(0xE220A8397B1DCDAFUL, random.NextUInt64());
            Assert.AreEqual(0x6E789E6AA1B965F4UL, random.NextUInt64());
            Assert.AreEqual(0x06C45D188009454FUL, random.NextUInt64());
        }

        [Test]
        public void NextDouble_IsTheTop53BitsScaled()
        {
            Assert.AreEqual(0.1330796686614273, new RoadRandom(12345).NextDouble(), 1e-15);
        }

        [Test]
        public void SameSeed_SameSequence_DifferentSeed_DifferentSequence()
        {
            var a = new RoadRandom(7);
            var b = new RoadRandom(7);
            var c = new RoadRandom(8);

            for (int i = 0; i < 100; i++)
            {
                ulong value = a.NextUInt64();
                Assert.AreEqual(value, b.NextUInt64());
                Assert.AreNotEqual(value, c.NextUInt64());
            }
        }

        [Test]
        public void NextDouble_StaysInTheHalfOpenUnitInterval_AndIsRoughlyUniform()
        {
            var random = new RoadRandom(99);
            double sum = 0.0;
            const int Count = 20000;
            for (int i = 0; i < Count; i++)
            {
                double value = random.NextDouble();
                Assert.GreaterOrEqual(value, 0.0);
                Assert.Less(value, 1.0);
                sum += value;
            }

            Assert.AreEqual(0.5, sum / Count, 0.01);
        }
    }
}
