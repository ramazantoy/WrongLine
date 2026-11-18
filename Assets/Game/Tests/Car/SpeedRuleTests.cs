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
            Assert.AreEqual(0.0, new SpeedRule(settings).Distance);
        }

        [TestCase(1f / 30f)]
        [TestCase(1f / 60f)]
        [TestCase(1f / 10f)]
        public void PullsAwayAtTheAcceleration_AndSettlesAtTheCruiseSpeedWithoutGain(float deltaTime)
        {
            var settings = new CarSettings();
            settings.Speed.GainPerSecond = 0f;
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

        [TestCase(1f / 30f)]
        [TestCase(1f / 60f)]
        [TestCase(1f / 10f)]
        public void KeepsGainingSpeedAfterTheCruiseSpeed_UpToTheTopSpeed_WhateverTheFrameTime(float deltaTime)
        {
            var settings = new CarSettings();
            var rule = new SpeedRule(settings);
            float rampSeconds = (settings.Speed.CruiseSpeed - settings.Speed.StartSpeed) / settings.Speed.Acceleration;
            float gainSeconds = (settings.Speed.MaxSpeed - settings.Speed.CruiseSpeed) / settings.Speed.GainPerSecond;

            // Halfway through the gaining stretch.
            float until = rampSeconds + gainSeconds * 0.5f;
            for (float t = 0f; t < until; t += deltaTime)
            {
                rule.Update(deltaTime);
            }

            float expected = settings.Speed.CruiseSpeed + (settings.Speed.MaxSpeed - settings.Speed.CruiseSpeed) * 0.5f;
            Assert.AreEqual(expected, rule.Speed, settings.Speed.GainPerSecond * deltaTime + 0.01f);

            for (float t = 0f; t < gainSeconds; t += deltaTime)
            {
                rule.Update(deltaTime);
                Assert.LessOrEqual(rule.Speed, settings.Speed.MaxSpeed + 1e-3f);
            }

            Assert.AreEqual(settings.Speed.MaxSpeed, rule.Speed, 1e-3f);
        }

        [Test]
        public void TheDistanceDoesNotDependOnTheFrameTime()
        {
            var fast = new SpeedRule(new CarSettings());
            var slow = new SpeedRule(new CarSettings());

            for (int i = 0; i < 600; i++)
            {
                fast.Update(1f / 60f);
            }

            for (int i = 0; i < 100; i++)
            {
                slow.Update(0.1f);
            }

            Assert.AreEqual(fast.Distance, slow.Distance, 0.05);
        }

        [Test]
        public void APause_KeepsTheSpeedAndTheDistance()
        {
            var rule = new SpeedRule(new CarSettings());

            rule.Update(0f);

            Assert.AreEqual(new CarSettings().Speed.StartSpeed, rule.Speed);
            Assert.AreEqual(0.0, rule.Distance);
        }

        [Test]
        public void ASettingsObjectWithATopSpeedBelowTheCruiseSpeedIsRejected()
        {
            var settings = new CarSettings();
            settings.Speed.MaxSpeed = settings.Speed.CruiseSpeed - 1f;

            Assert.Throws<System.ArgumentException>(() => new SpeedRule(settings));
        }
    }
}
