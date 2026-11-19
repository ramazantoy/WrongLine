using NUnit.Framework;
using WrongLine.UI;

namespace WrongLine.Tests.UI
{
    public sealed class CountUpRuleTests
    {
        [Test]
        public void StartsAtZero()
        {
            Assert.AreEqual(0, CountUpRule.Value(1000, 0f, 2f));
            Assert.AreEqual(0, CountUpRule.Value(1000, -1f, 2f));
        }

        [Test]
        public void EndsAtTheTarget()
        {
            Assert.AreEqual(1000, CountUpRule.Value(1000, 2f, 2f));
            Assert.AreEqual(1000, CountUpRule.Value(1000, 9f, 2f));
        }

        [Test]
        public void Rises_AndNeverPassesTheTarget()
        {
            int last = -1;
            for (float t = 0f; t <= 2f; t += 0.05f)
            {
                int value = CountUpRule.Value(1000, t, 2f);
                Assert.GreaterOrEqual(value, last);
                Assert.LessOrEqual(value, 1000);
                last = value;
            }
        }

        [Test]
        public void IsFastAtFirst()
        {
            // Ease-out: past half the target by a third of the time.
            Assert.Greater(CountUpRule.Value(1000, 0.7f, 2f), 500);
        }

        [Test]
        public void NothingToCount_IsZero()
        {
            Assert.AreEqual(0, CountUpRule.Value(0, 1f, 2f));
        }

        [Test]
        public void ZeroDuration_ShowsTheTargetAtOnce()
        {
            Assert.AreEqual(42, CountUpRule.Value(42, 0f, 0f));
        }
    }
}
