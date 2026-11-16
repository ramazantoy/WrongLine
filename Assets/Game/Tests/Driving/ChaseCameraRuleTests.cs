using System;
using NUnit.Framework;
using UnityEngine;

namespace WrongLine.Driving.Tests
{
    public class ChaseCameraRuleTests
    {
        private static ChaseCameraSettings Settings(Action<ChaseCameraSettings> change = null)
        {
            var settings = new ChaseCameraSettings();
            change?.Invoke(settings);
            return settings;
        }

        [Test]
        public void TheFirstUpdate_SnapsBehindAndAboveTheTarget_LookingAhead()
        {
            ChaseCameraSettings settings = Settings();
            var rule = new ChaseCameraRule(settings);

            rule.Update(new Vector3(10f, 2f, 50f), Quaternion.identity, 0.016f);

            Vector3 expected = new Vector3(10f, 2f + settings.Height, 50f - settings.Distance);
            Assert.AreEqual(0f, Vector3.Distance(expected, rule.Position), 1e-4f);
            Vector3 toLookAt = new Vector3(10f, 2f, 50f + settings.LookAhead) - rule.Position;
            Assert.Less(Vector3.Angle(toLookAt, rule.Rotation * Vector3.forward), 0.01f);
        }

        [Test]
        public void OffsetFollowsTheTargetsHeading()
        {
            var rule = new ChaseCameraRule(Settings());
            Quaternion turnedRight = Quaternion.Euler(0f, 90f, 0f);

            rule.Update(Vector3.zero, turnedRight, 0.016f);

            Assert.Less(rule.Position.x, -7f, "Behind a target facing +X is at -X.");
            Assert.AreEqual(0f, rule.Position.z, 1e-3f);
        }

        [Test]
        public void ATargetThatStandsStill_IsCaughtUpWith()
        {
            ChaseCameraSettings settings = Settings();
            var rule = new ChaseCameraRule(settings);
            rule.Update(Vector3.zero, Quaternion.identity, 0.016f);

            Vector3 target = new Vector3(0f, 0f, 100f);
            for (int i = 0; i < 600; i++)
            {
                rule.Update(target, Quaternion.identity, 1f / 60f);
            }

            Assert.AreEqual(0f, Vector3.Distance(new Vector3(0f, settings.Height, 100f - settings.Distance), rule.Position), 1e-3f);
        }

        [TestCase(1f / 60f)]
        [TestCase(1f / 30f)]
        [TestCase(1f / 10f)]
        public void CatchingUpWithAJump_TakesTheSameTimeOnAnyFrameRate(float deltaTime)
        {
            ChaseCameraSettings settings = Settings();
            var rule = new ChaseCameraRule(settings);
            rule.Update(Vector3.zero, Quaternion.identity, deltaTime);
            var jumped = new Vector3(0f, 0f, 10f);

            int steps = Mathf.RoundToInt(0.5f / deltaTime);
            for (int i = 0; i < steps; i++)
            {
                rule.Update(jumped, Quaternion.identity, deltaTime);
            }

            float gap = (10f - settings.Distance) - rule.Position.z;
            float expected = 10f * Mathf.Exp(-settings.PositionSharpness * 0.5f);
            Assert.AreEqual(expected, gap, 1e-3f, "The remaining gap after half a second is the same whatever the frame rate.");
        }

        [TestCase(1f / 60f)]
        [TestCase(1f / 20f)]
        public void WhileMoving_TrailsBySpeedOverSharpness_LessHalfAFrameOfTravel(float deltaTime)
        {
            ChaseCameraSettings settings = Settings();
            var rule = new ChaseCameraRule(settings);
            const float Speed = 30f;
            float z = 0f;
            rule.Update(new Vector3(0f, 0f, z), Quaternion.identity, deltaTime);

            for (float t = 0f; t < 6f; t += deltaTime)
            {
                z += Speed * deltaTime;
                rule.Update(new Vector3(0f, 0f, z), Quaternion.identity, deltaTime);
            }

            float trail = (z - settings.Distance) - rule.Position.z;
            float exact = Speed * deltaTime / (Mathf.Exp(settings.PositionSharpness * deltaTime) - 1f);
            Assert.AreEqual(exact, trail, 0.01f, "The steady lag of one catch-up step per frame.");
            Assert.AreEqual(Speed / settings.PositionSharpness - Speed * deltaTime / 2f, trail, 0.1f, "About speed over sharpness, minus half a frame of travel.");
        }

        [Test]
        public void ZeroDeltaTime_ChangesNothing_AfterTheFirstUpdate()
        {
            var rule = new ChaseCameraRule(Settings());
            rule.Update(Vector3.zero, Quaternion.identity, 0.016f);
            Vector3 position = rule.Position;
            Quaternion rotation = rule.Rotation;

            rule.Update(new Vector3(50f, 0f, 500f), Quaternion.Euler(0f, 60f, 0f), 0f);

            Assert.AreEqual(position, rule.Position);
            Assert.AreEqual(rotation, rule.Rotation);
        }

        [Test]
        public void ShiftingTheOrigin_MovesTheCameraByTheSameAmount()
        {
            var rule = new ChaseCameraRule(Settings());
            rule.Update(new Vector3(100f, 0f, 400f), Quaternion.identity, 0.016f);
            Vector3 before = rule.Position;

            rule.Shift(new Vector3(100f, 0f, 400f));

            Assert.AreEqual(0f, Vector3.Distance(before - new Vector3(100f, 0f, 400f), rule.Position), 1e-4f);
        }

        [Test]
        public void UpBlend_DecidesHowMuchTheCameraLeansWithTheBank()
        {
            Quaternion banked = Quaternion.Euler(0f, 0f, 30f);

            var level = new ChaseCameraRule(Settings(s => s.UpBlend = 0f));
            level.Update(Vector3.zero, banked, 0.016f);
            var leaning = new ChaseCameraRule(Settings(s => s.UpBlend = 1f));
            leaning.Update(Vector3.zero, banked, 0.016f);

            // Roll is how far the camera's right side leaves the horizontal (its pitch from looking down does not count).
            Assert.Less(Mathf.Abs((level.Rotation * Vector3.right).y), 0.01f, "Kept level: no roll.");
            Assert.Greater(Mathf.Abs((leaning.Rotation * Vector3.right).y), 0.3f, "Leaning with a 30 degree bank rolls the camera (sin 30 = 0.5).");
        }

        [Test]
        public void NonsenseSettings_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new ChaseCameraRule(Settings(s => s.PositionSharpness = 0f)));
            Assert.Throws<ArgumentException>(() => new ChaseCameraRule(Settings(s => s.Distance = -1f)));
            Assert.Throws<ArgumentException>(() => new ChaseCameraRule(Settings(s => s.UpBlend = 2f)));
        }
    }
}
