using NUnit.Framework;
using WrongLine.UI;

namespace WrongLine.Tests.UI
{
    public sealed class CountTickRuleTests
    {
        [Test]
        public void TheFirstChangeTicksAtOnce_ThenNoFasterThanTheInterval()
        {
            var rule = new CountTickRule(0.05f);
            int ticks = 0;

            for (int frame = 0; frame < 60; frame++) // One second at 60 fps, the number moving every frame.
            {
                ticks += rule.Step(1f / 60f, true) ? 1 : 0;
            }

            Assert.IsTrue(new CountTickRule(0.05f).Step(0f, true));
            Assert.AreEqual(20, ticks, 1);
        }

        [Test]
        public void NothingTicksWhileTheNumberStandsStill()
        {
            var rule = new CountTickRule(0.05f);

            for (int frame = 0; frame < 30; frame++)
            {
                Assert.IsFalse(rule.Step(1f / 60f, false));
            }
        }
    }
}
