using NUnit.Framework;

namespace WrongLine.Traffic.Tests
{
    public class PaintKeyTests
    {
        [Test]
        public void TheKeyMagenta_IsBodyPaint_AtAnyBrightness()
        {
            Assert.AreEqual(1f, PaintKey.Weight(1f, 0f, 1f), 1e-4f);
            Assert.AreEqual(1f, PaintKey.Weight(0.9f, 0.1f, 0.85f), 1e-4f, "A slightly off magenta still keys fully.");
            Assert.AreEqual(1f, PaintKey.Weight(0.45f, 0.02f, 0.4f), 1e-4f, "A shaded body keys as well as a lit one.");
        }

        [Test]
        public void EveryOtherPartOfTheCar_IsNotBodyPaint()
        {
            Assert.AreEqual(0f, PaintKey.Weight(0.6f, 0.6f, 0.62f), "Glass grey.");
            Assert.AreEqual(0f, PaintKey.Weight(1f, 0.95f, 0.8f), "Headlight warm white.");
            Assert.AreEqual(0f, PaintKey.Weight(0.9f, 0.1f, 0.1f), "Stop light red.");
            Assert.AreEqual(0f, PaintKey.Weight(0.95f, 0.6f, 0.1f), "Turn signal amber.");
            Assert.AreEqual(0f, PaintKey.Weight(0.08f, 0.08f, 0.08f), "Tyre black.");
            Assert.AreEqual(0f, PaintKey.Weight(0f, 0f, 0f), "Nothing.");
        }

        [Test]
        public void TheEdgeBetweenPaintAndAnotherPart_BlendsWithoutAJump()
        {
            // Magenta fading into grey, step by step: the weight never falls from 1 to 0 in one step.
            float previous = 1f;
            for (int i = 0; i <= 20; i++)
            {
                float t = i / 20f;
                float weight = PaintKey.Weight(1f - 0.4f * t, 0.6f * t, 1f - 0.4f * t);

                Assert.LessOrEqual(previous - weight, 0.35f, $"Step {i}");
                Assert.GreaterOrEqual(weight, 0f);
                Assert.LessOrEqual(weight, 1f);
                previous = weight;
            }
        }

        [Test]
        public void NeutralColour_TurnsTheKeyIntoAGreyOfItsShade_AndLeavesTheRestAlone()
        {
            PaintKey.NeutralColour(1f, 0f, 1f, out float r, out float g, out float b);
            Assert.AreEqual(PaintKey.NeutralGrey, r, 1e-4f);
            Assert.AreEqual(r, g, 1e-4f);
            Assert.AreEqual(r, b, 1e-4f);

            PaintKey.NeutralColour(0.5f, 0.02f, 0.5f, out r, out g, out b);
            Assert.AreEqual(0.5f * PaintKey.NeutralGrey, g, 1e-4f, "A shaded texel stays shaded.");

            PaintKey.NeutralColour(0.1f, 0.1f, 0.1f, out r, out g, out b);
            Assert.AreEqual(0.1f, r, 1e-6f);
            Assert.AreEqual(0.1f, g, 1e-6f);
            Assert.AreEqual(0.1f, b, 1e-6f);

            PaintKey.NeutralColour(0.95f, 0.6f, 0.1f, out r, out g, out b);
            Assert.AreEqual(0.95f, r, 1e-6f, "Amber is not touched.");
            Assert.AreEqual(0.1f, b, 1e-6f);
        }
    }
}
