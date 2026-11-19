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
        public void APaleYellowGeneratedHeadlight_IsAHeadlight_ButAmberAndGreyAreNot()
        {
            Assert.AreEqual(LightPanel.Headlight, LightMaskKey.Classify(0.92f, 0.86f, 0.55f));
            Assert.AreEqual(LightPanel.Signal, LightMaskKey.Classify(0.95f, 0.62f, 0.12f));
            Assert.AreEqual(LightPanel.None, LightMaskKey.Classify(0.82f, 0.82f, 0.4f), "Too dark to be a lit panel.");
        }

        [Test]
        public void SaturatedRed_IsAStopLight()
        {
            Assert.AreEqual(LightPanel.Stop, LightMaskKey.Classify(1f, 0.05f, 0.05f));
        }

        [Test]
        public void ADarkerGeneratedRed_IsAStopLight_ButNotADarkRedOrAMagenta()
        {
            Assert.AreEqual(LightPanel.Stop, LightMaskKey.Classify(0.70f, 0.03f, 0.06f));
            Assert.AreEqual(LightPanel.None, LightMaskKey.Classify(0.5f, 0.03f, 0.05f), "Too dark.");
            Assert.AreEqual(LightPanel.None, LightMaskKey.Classify(0.9f, 0.05f, 0.85f), "The traffic body's key magenta.");
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
