using NUnit.Framework;

namespace WrongLine.Car.Tests
{
    public class CarPoseRuleTests
    {
        private const float Dt = 1f / 60f;

        [Test]
        public void MovingRight_TurnsTheNoseTheWheelAndTheFrontWheelsRight()
        {
            var rule = new CarPoseRule(new CarSettings());

            rule.Update(6f, 30f, Dt);

            Assert.Greater(rule.YawDegrees, 0f);
            Assert.Greater(rule.SteeringWheelDegrees, 0f);
            Assert.Greater(rule.FrontWheelSteerDegrees, 0f);
        }

        [Test]
        public void MovingLeft_IsTheMirrorImage()
        {
            var right = new CarPoseRule(new CarSettings());
            var left = new CarPoseRule(new CarSettings());

            right.Update(6f, 30f, Dt);
            left.Update(-6f, 30f, Dt);

            Assert.AreEqual(-right.YawDegrees, left.YawDegrees, 1e-4f);
            Assert.AreEqual(-right.SteeringWheelDegrees, left.SteeringWheelDegrees, 1e-4f);
            Assert.AreEqual(-right.FrontWheelSteerDegrees, left.FrontWheelSteerDegrees, 1e-4f);
        }

        [Test]
        public void TheAngles_StayWithinTheirLimits()
        {
            var settings = new CarSettings();
            var rule = new CarPoseRule(settings);

            rule.Update(1000f, 1f, Dt);

            Assert.AreEqual(settings.Pose.MaxYawDegrees, rule.YawDegrees, 1e-4f);
            Assert.AreEqual(settings.Pose.MaxWheelDegrees, rule.SteeringWheelDegrees, 1e-4f);
            Assert.AreEqual(settings.Pose.MaxSteerDegrees, rule.FrontWheelSteerDegrees, 1e-4f);
        }

        [Test]
        public void SpeedingUpToTheRight_LeansTheBodyLeft_AndItSettlesBackUpright()
        {
            var rule = new CarPoseRule(new CarSettings());
            rule.Update(0f, 30f, Dt);

            rule.Update(5f, 30f, Dt);
            Assert.Less(rule.RollDegrees, 0f, "Leaning away from the turn: left, a negative roll.");

            for (int i = 0; i < 120; i++)
            {
                rule.Update(5f, 30f, Dt);
            }

            Assert.AreEqual(0f, rule.RollDegrees, 0.01f);
        }

        [Test]
        public void TheRoll_NeverPassesItsLimit()
        {
            var settings = new CarSettings();
            var rule = new CarPoseRule(settings);
            rule.Update(0f, 30f, Dt);

            for (int i = 1; i < 200; i++)
            {
                rule.Update(i % 2 == 0 ? 14f : -14f, 30f, Dt);
                Assert.LessOrEqual(System.Math.Abs(rule.RollDegrees), settings.Pose.MaxRollDegrees + 1e-4f);
            }
        }

        [Test]
        public void GoingStraight_LooksStraight()
        {
            var rule = new CarPoseRule(new CarSettings());

            rule.Update(0f, 30f, Dt);

            Assert.AreEqual(0f, rule.YawDegrees);
            Assert.AreEqual(0f, rule.RollDegrees);
            Assert.AreEqual(0f, rule.SteeringWheelDegrees);
            Assert.AreEqual(0f, rule.FrontWheelSteerDegrees);
        }

        [Test]
        public void TheWheels_SpinWithTheSpeed_AndWrap()
        {
            var settings = new CarSettings();
            var rule = new CarPoseRule(settings);

            rule.Update(0f, 10f, 0.1f);

            // 10 m/s for 0.1 s is 1 m of road, 1 / radius radians.
            Assert.AreEqual(1f / settings.Pose.WheelRadius * 57.29578f, rule.WheelSpinDegrees, 1e-2f);

            for (int i = 0; i < 1000; i++)
            {
                rule.Update(0f, 30f, Dt);
                Assert.GreaterOrEqual(rule.WheelSpinDegrees, 0f);
                Assert.Less(rule.WheelSpinDegrees, 360f);
            }
        }

        [Test]
        public void APause_ChangesNeitherTheSpinNorTheRoll()
        {
            var rule = new CarPoseRule(new CarSettings());
            rule.Update(0f, 30f, Dt);
            rule.Update(5f, 30f, Dt);
            float roll = rule.RollDegrees;
            float spin = rule.WheelSpinDegrees;

            rule.Update(5f, 30f, 0f);

            Assert.AreEqual(roll, rule.RollDegrees);
            Assert.AreEqual(spin, rule.WheelSpinDegrees);
        }
    }
}
