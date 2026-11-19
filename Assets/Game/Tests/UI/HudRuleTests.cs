using NUnit.Framework;
using WrongLine.UI;

namespace WrongLine.Tests.UI
{
    public sealed class HudRuleTests
    {
        [Test]
        public void FirstFrame_AlwaysChanged()
        {
            var rule = new HudRule(0);

            var frame = rule.Update(0);

            Assert.IsTrue(frame.Changed);
            Assert.AreEqual(0, frame.Meters);
        }

        [Test]
        public void SameWholeMetre_IsNotRewritten()
        {
            var rule = new HudRule(0);
            rule.Update(10.1);

            Assert.IsFalse(rule.Update(10.9).Changed);
            Assert.IsTrue(rule.Update(11.0).Changed);
        }

        [Test]
        public void Metres_RoundDown()
        {
            Assert.AreEqual(1284, new HudRule(0).Update(1284.99).Meters);
        }

        [Test]
        public void NegativeOrNaN_ShowZero()
        {
            Assert.AreEqual(0, new HudRule(0).Update(-5).Meters);
            Assert.AreEqual(0, new HudRule(0).Update(double.NaN).Meters);
        }

        [Test]
        public void Record_IsAnnouncedOnce_WhenTheBestIsPassed()
        {
            var rule = new HudRule(100);

            Assert.IsFalse(rule.Update(100).NewRecord, "Equal to the best is not past it.");
            Assert.IsTrue(rule.Update(101).NewRecord);
            Assert.IsFalse(rule.Update(102).NewRecord);
            Assert.IsFalse(rule.Update(500).NewRecord);
        }

        [Test]
        public void FirstRun_NeverAnnouncesARecord()
        {
            var rule = new HudRule(0);

            Assert.IsFalse(rule.Update(5000).NewRecord);
        }
    }
}
