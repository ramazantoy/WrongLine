using NUnit.Framework;
using WrongLine.Haptics;

namespace WrongLine.Tests.Haptics
{
    public sealed class HapticRuleTests
    {
        [Test]
        public void Disabled_NeverPlays()
        {
            var rule = new HapticRule(0.1f);

            Assert.IsFalse(rule.Allow(HapticKind.Crash, 0f, false));
        }

        [Test]
        public void FirstPulse_Plays()
        {
            Assert.IsTrue(new HapticRule(0.1f).Allow(HapticKind.Tick, 0f, true));
        }

        [Test]
        public void SameKindTooSoon_IsDropped_AndPlaysAfterTheGap()
        {
            var rule = new HapticRule(0.1f);
            rule.Allow(HapticKind.Tap, 0f, true);

            Assert.IsFalse(rule.Allow(HapticKind.Tap, 0.05f, true));
            Assert.IsTrue(rule.Allow(HapticKind.Tap, 0.11f, true));
        }

        [Test]
        public void LighterKindTooSoon_IsDropped()
        {
            var rule = new HapticRule(0.1f);
            rule.Allow(HapticKind.Bump, 0f, true);

            Assert.IsFalse(rule.Allow(HapticKind.Tick, 0.02f, true));
        }

        [Test]
        public void HarderKind_AlwaysPlays()
        {
            var rule = new HapticRule(0.1f);
            rule.Allow(HapticKind.Tick, 0f, true);

            Assert.IsTrue(rule.Allow(HapticKind.Crash, 0.01f, true));
        }

        [Test]
        public void ADroppedPulse_DoesNotPushTheGapBack()
        {
            var rule = new HapticRule(0.1f);
            rule.Allow(HapticKind.Tick, 0f, true);
            rule.Allow(HapticKind.Tick, 0.08f, true); // Dropped.

            Assert.IsTrue(rule.Allow(HapticKind.Tick, 0.11f, true), "The gap counts from the last pulse that played.");
        }
    }
}
