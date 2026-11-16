using NUnit.Framework;

namespace WrongLine.Car.Tests
{
    public class SpeedRuleTests
    {
        [Test]
        public void StartsAtTheStartSpeed()
        {
            var settings = new CarSettings();

            Assert.AreEqual(settings.Speed.StartSpeed, new SpeedRule(settings).Speed);
        }

        [TestCase(1f / 30f)]
        [TestCase(1f / 60f)]
        [TestCase(1f / 10f)]
        public void PullsAwayAtTheAcceleration_AndSettlesAtTheCruiseSpeed(float deltaTime)
        {
            var settings = new CarSettings();
            var rule = new SpeedRule(settings);

            float elapsed = 0f;
            while (rule.Speed < settings.Speed.CruiseSpeed)
            {
                float before = rule.Speed;
                rule.Update(deltaTime);
                elapsed += deltaTime;
                Assert.Greater(rule.Speed, before);
            }

            float expected = (settings.Speed.CruiseSpeed - settings.Speed.StartSpeed) / settings.Speed.Acceleration;
            Assert.AreEqual(expected, elapsed, deltaTime + 1e-4f);

            for (int i = 0; i < 100; i++)
            {
                rule.Update(deltaTime);
            }

            Assert.AreEqual(settings.Speed.CruiseSpeed, rule.Speed);
        }

        [Test]
        public void APause_KeepsTheSpeed()
        {
            var rule = new SpeedRule(new CarSettings());

            rule.Update(0f);

            Assert.AreEqual(new CarSettings().Speed.StartSpeed, rule.Speed);
        }
    }
}
