using NUnit.Framework;

namespace WrongLine.Car.Tests
{
    public class SideswipeReactionTests
    {
        private const float Frame = 1f / 60f;

        private CarSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new CarSettings();
        }

        [Test]
        public void APushMovesTheTargetAwayAndGivesTheCarSidewaysSpeed()
        {
            var lateral = new LateralRule(_settings);
            float start = lateral.TargetX;

            lateral.Push(-1, 0.8f, 5f);

            Assert.AreEqual(start - 0.8f, lateral.TargetX, 1e-4f);
            Assert.AreEqual(-5f, lateral.Velocity, 1e-4f);
        }

        [Test]
        public void APushNeverTakesTheTargetOffTheRoad()
        {
            var lateral = new LateralRule(_settings);
            for (int i = 0; i < 10; i++)
            {
                lateral.Push(1, 3f, 100f);
            }

            Assert.AreEqual(_settings.LateralLimit, lateral.TargetX, 1e-4f);
            Assert.AreEqual(_settings.Lateral.MaxLateralSpeed, lateral.Velocity, 1e-4f);
        }

        [Test]
        public void TheCarEndsUpAtThePushedTarget()
        {
            var lateral = new LateralRule(_settings);
            lateral.Push(1, 0.8f, 5f);
            float target = lateral.TargetX;

            for (int i = 0; i < 120; i++)
            {
                lateral.Update(0f, 0f, Frame);
            }

            Assert.AreEqual(target, lateral.X, 0.01f);
        }

        [Test]
        public void ThePushDoesNotTouchTheSpeedAlongTheRoad()
        {
            // The traffic was laid out for the player's planned speed: a sideswipe must leave it alone.
            var speed = new SpeedRule(_settings);
            var lateral = new LateralRule(_settings);
            speed.Update(1f);
            float before = speed.Speed;

            lateral.Push(1, 0.8f, 5f);

            Assert.AreEqual(before, speed.Speed);
        }

        [Test]
        public void TheKickTurnsTheNoseTowardsThePushAndLeansTheBodyAwayThenFades()
        {
            var pose = new CarPoseRule(_settings);
            pose.Update(0f, 30f, Frame);

            pose.Kick(1, 6f, 4f, 0.35f);
            pose.Update(0f, 30f, Frame);
            Assert.Greater(pose.YawDegrees, 5f);
            Assert.Less(pose.RollDegrees, -3f);

            for (int i = 0; i < 60; i++)
            {
                pose.Update(0f, 30f, Frame);
            }

            Assert.AreEqual(0f, pose.YawDegrees, 1e-3f);
            Assert.AreEqual(0f, pose.RollDegrees, 1e-3f);
        }

        [Test]
        public void TheKickDoesNotPileUpInTheRoll()
        {
            // The roll is smoothed from frame to frame; the kick must sit on top of it, not feed into it.
            var pose = new CarPoseRule(_settings);
            pose.Update(0f, 30f, Frame);
            pose.Kick(1, 6f, 4f, 0.35f);
            pose.Update(0f, 30f, Frame);
            float first = pose.RollDegrees;

            pose.Kick(1, 6f, 4f, 0.35f);
            pose.Update(0f, 30f, Frame);

            Assert.AreEqual(first, pose.RollDegrees, 0.5f);
        }
    }
}
