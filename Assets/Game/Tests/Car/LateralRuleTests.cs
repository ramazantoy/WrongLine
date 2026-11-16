using NUnit.Framework;
using UnityEngine;

namespace WrongLine.Car.Tests
{
    public class LateralRuleTests
    {
        private static void Run(LateralRule rule, float seconds, float deltaTime)
        {
            int steps = Mathf.RoundToInt(seconds / deltaTime);
            for (int i = 0; i < steps; i++)
            {
                rule.Update(0f, 0f, deltaTime);
            }
        }

        [Test]
        public void ADrag_MovesTheTargetByItsShareOfTheScreen()
        {
            var settings = new CarSettings();
            var rule = new LateralRule(settings);

            rule.Update(0.2f, 0f, 0f);

            Assert.AreEqual(0.2f * settings.Lateral.DragMetersPerScreen, rule.TargetX, 1e-5f);
            Assert.AreEqual(0f, rule.X, "The car has not moved yet: no time passed.");
        }

        [Test]
        public void AHeldDirection_MovesTheTargetAtTheAxisSpeed()
        {
            var settings = new CarSettings();
            var rule = new LateralRule(settings);

            rule.Update(0f, -1f, 0.25f);

            Assert.AreEqual(-settings.Lateral.AxisSpeed * 0.25f, rule.TargetX, 1e-5f);
        }

        [Test]
        public void TheTargetAndTheCar_NeverLeaveTheRoad()
        {
            var settings = new CarSettings();
            var rule = new LateralRule(settings);
            float limit = settings.LateralLimit;

            for (int i = 0; i < 600; i++)
            {
                rule.Update(5f, 0f, 1f / 60f);
                Assert.LessOrEqual(rule.TargetX, limit + 1e-5f);
                Assert.LessOrEqual(rule.X, limit + 1e-5f);
            }

            Assert.AreEqual(limit, rule.TargetX, 1e-5f);
            Assert.AreEqual(limit, rule.X, 0.01f);

            for (int i = 0; i < 600; i++)
            {
                rule.Update(-5f, 0f, 1f / 60f);
                Assert.GreaterOrEqual(rule.X, -limit - 1e-5f);
            }
        }

        [TestCase(1f / 30f)]
        [TestCase(1f / 60f)]
        [TestCase(1f / 120f)]
        [TestCase(1f / 15f)]
        public void ReachingATarget_TakesTheSameTimeOnAnyFrameRate(float deltaTime)
        {
            var settings = new CarSettings();
            var rule = new LateralRule(settings);
            rule.Update(2f / settings.Lateral.DragMetersPerScreen, 0f, 0f); // Target 2 m, set in one go.

            Run(rule, 0.2f, deltaTime);

            // A critically damped spring from rest: x(t) = T (1 - (1 + wt) e^-wt).
            float omega = 2f / settings.Lateral.SmoothTime;
            float expected = 2f * (1f - (1f + omega * 0.2f) * Mathf.Exp(-omega * 0.2f));
            Assert.AreEqual(expected, rule.X, 0.02f);
        }

        [Test]
        public void TheSidewaysSpeed_NeverPassesTheLimit()
        {
            var settings = new CarSettings();
            var rule = new LateralRule(settings);
            rule.Update(5f, 0f, 0f);

            for (int i = 0; i < 120; i++)
            {
                rule.Update(0f, 0f, 1f / 60f);
                Assert.LessOrEqual(Mathf.Abs(rule.Velocity), settings.Lateral.MaxLateralSpeed + 1e-4f);
            }
        }

        [Test]
        public void ANeverEndingPause_ChangesNothing()
        {
            var rule = new LateralRule(new CarSettings());
            rule.Update(0.2f, 0f, 0f);
            Run(rule, 0.1f, 1f / 60f);
            float x = rule.X;
            float velocity = rule.Velocity;

            for (int i = 0; i < 100; i++)
            {
                rule.Update(0f, 0f, 0f);
            }

            Assert.AreEqual(x, rule.X);
            Assert.AreEqual(velocity, rule.Velocity);
        }
    }
}
