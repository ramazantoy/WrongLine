using NUnit.Framework;

namespace WrongLine.Traffic.Tests
{
    public class TrafficLodRuleTests
    {
        private static TrafficLodRule Rule()
        {
            return new TrafficLodRule(new TrafficSettings.LodGroup { NearMeters = 35f, FarMeters = 120f, HysteresisMeters = 4f });
        }

        [Test]
        public void ANewCarGetsTheLevelOfItsDistance()
        {
            TrafficLodRule rule = Rule();

            Assert.AreEqual(0, rule.Evaluate(10f, TrafficLodRule.None));
            Assert.AreEqual(1, rule.Evaluate(60f, TrafficLodRule.None));
            Assert.AreEqual(2, rule.Evaluate(300f, TrafficLodRule.None));
        }

        [Test]
        public void ACarGoesDownAtTheLimit()
        {
            TrafficLodRule rule = Rule();

            Assert.AreEqual(1, rule.Evaluate(35f, 0));
            Assert.AreEqual(2, rule.Evaluate(120f, 1));
            Assert.AreEqual(2, rule.Evaluate(120f, 0));
        }

        [Test]
        public void ACarDoesNotGoBackUpUntilItIsInsideTheLimitByTheHysteresis()
        {
            TrafficLodRule rule = Rule();

            Assert.AreEqual(1, rule.Evaluate(34f, 1), "just inside 35 is not enough");
            Assert.AreEqual(1, rule.Evaluate(31.5f, 1));
            Assert.AreEqual(0, rule.Evaluate(30.9f, 1));
            Assert.AreEqual(2, rule.Evaluate(118f, 2));
            Assert.AreEqual(1, rule.Evaluate(115f, 2));
        }

        [Test]
        public void ACarSittingAtALimit_DoesNotFlicker()
        {
            TrafficLodRule rule = Rule();
            int level = rule.Evaluate(60f, TrafficLodRule.None);
            int changes = 0;

            foreach (float distance in new[] { 36f, 34.9f, 35.1f, 34.2f, 35.4f, 33.5f, 35.2f })
            {
                int next = rule.Evaluate(distance, level);
                if (next != level)
                {
                    changes++;
                }

                level = next;
            }

            Assert.LessOrEqual(changes, 1);
        }

        [Test]
        public void ACarFarAwayFromCloseByDropsStraightToLevelTwo()
        {
            Assert.AreEqual(2, Rule().Evaluate(200f, 0));
        }
    }
}
