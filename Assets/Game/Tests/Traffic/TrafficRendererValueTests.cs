using NUnit.Framework;

namespace WrongLine.Traffic.Tests
{
    public class TrafficRendererValueTests
    {
        [Test]
        public void PackedValue_GivesBackThePaintAndBothSignals()
        {
            for (int paint = 0; paint < TrafficRendererValue.MaxPaints; paint++)
            {
                foreach (bool left in new[] { false, true })
                {
                    foreach (bool right in new[] { false, true })
                    {
                        uint value = TrafficRendererValue.Pack(paint, left, right);

                        Assert.AreEqual(paint, TrafficRendererValue.Paint(value));
                        Assert.AreEqual(left, TrafficRendererValue.LeftSignal(value));
                        Assert.AreEqual(right, TrafficRendererValue.RightSignal(value));
                    }
                }
            }
        }

        [Test]
        public void TheSignalBits_AreKeptOutOfThePaintIndex()
        {
            Assert.AreEqual(0, TrafficRendererValue.Paint(TrafficRendererValue.Pack(0, true, true)));
            Assert.AreEqual(5u, TrafficRendererValue.Pack(5, false, false), "The shader reads the paint from the low four bits.");
            Assert.AreEqual(16u, TrafficRendererValue.Pack(0, true, false), "Left is bit 4.");
            Assert.AreEqual(32u, TrafficRendererValue.Pack(0, false, true), "Right is bit 5.");
        }

        [Test]
        public void APaintBeyondTheRange_DoesNotReachTheSignalBits()
        {
            uint value = TrafficRendererValue.Pack(TrafficRendererValue.MaxPaints + 3, false, false);

            Assert.IsFalse(TrafficRendererValue.LeftSignal(value));
            Assert.IsFalse(TrafficRendererValue.RightSignal(value));
        }
    }
}
