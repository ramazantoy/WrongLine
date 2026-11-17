using NUnit.Framework;

namespace WrongLine.World.Tests
{
    public class LampPoolFalloffTests
    {
        private const float Radius = 7f;
        private const float Vertical = 9f;

        [Test]
        public void UnderTheLamp_TheLightIsFull()
        {
            Assert.AreEqual(1f, LampPoolFalloff.Weight(0f, 5f, Radius, Vertical), 1e-5f);
        }

        [Test]
        public void AtAndBeyondTheRadius_ThereIsNoLight()
        {
            Assert.AreEqual(0f, LampPoolFalloff.Weight(Radius, 5f, Radius, Vertical), 1e-5f);
            Assert.AreEqual(0f, LampPoolFalloff.Weight(Radius * 2f, 5f, Radius, Vertical));
        }

        [Test]
        public void AcrossTheGround_TheLightOnlyFalls()
        {
            float last = float.MaxValue;
            for (float distance = 0f; distance <= Radius; distance += 0.25f)
            {
                float weight = LampPoolFalloff.Weight(distance, 5f, Radius, Vertical);
                Assert.LessOrEqual(weight, last + 1e-6f, $"at {distance} m");
                last = weight;
            }
        }

        [Test]
        public void AboveTheLampHead_OrFarBelowIt_ThereIsNoLight()
        {
            Assert.AreEqual(0f, LampPoolFalloff.Weight(1f, -2f, Radius, Vertical), "A surface above the head is not lit by it.");
            Assert.AreEqual(0f, LampPoolFalloff.Weight(1f, Vertical + 2f, Radius, Vertical), "Further below than the reach (the road far down a slope).");
        }

        [Test]
        public void TheVerticalEdges_AreSoft()
        {
            float nearTop = LampPoolFalloff.Weight(0f, 0.5f, Radius, Vertical);
            float nearBottom = LampPoolFalloff.Weight(0f, Vertical - 0.5f, Radius, Vertical);

            Assert.That(nearTop, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(nearBottom, Is.GreaterThan(0f).And.LessThan(1f));
        }
    }
}
