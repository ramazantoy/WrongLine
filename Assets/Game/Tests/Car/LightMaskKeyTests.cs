using NUnit.Framework;

namespace WrongLine.Car.Tests
{
    public class LightMaskKeyTests
    {
        [Test]
        public void WarmWhite_IsAHeadlight()
        {
            Assert.AreEqual(LightPanel.Headlight, LightMaskKey.Classify(1f, 0.97f, 0.88f));
        }

        [Test]
        public void SaturatedRed_IsAStopLight()
        {
            Assert.AreEqual(LightPanel.Stop, LightMaskKey.Classify(1f, 0.05f, 0.05f));
        }

        [Test]
        public void Amber_IsASignal()
        {
            Assert.AreEqual(LightPanel.Signal, LightMaskKey.Classify(0.95f, 0.62f, 0.12f));
        }

        [Test]
        public void TheBodyPaint_IsNoLight()
        {
            // The painted body: a muted red, not the saturated red of the stop light.
            Assert.AreEqual(LightPanel.None, LightMaskKey.Classify(0.72f, 0.31f, 0.3f));
        }

        [Test]
        public void GreyGlass_IsNoLight()
        {
            Assert.AreEqual(LightPanel.None, LightMaskKey.Classify(0.72f, 0.72f, 0.74f));
        }

        [Test]
        public void Black_IsNoLight()
        {
            Assert.AreEqual(LightPanel.None, LightMaskKey.Classify(0.05f, 0.05f, 0.05f));
        }
    }
}
