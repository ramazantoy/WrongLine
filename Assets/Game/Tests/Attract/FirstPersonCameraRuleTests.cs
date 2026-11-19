using System;
using NUnit.Framework;
using UnityEngine;

namespace WrongLine.Attract.Tests
{
    public class FirstPersonCameraRuleTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void TheEyeIsAboveAndAheadOfTheBodyAlongItsOwnAxes()
        {
            var rule = new FirstPersonCameraRule(new FirstPersonCameraSettings { EyeHeight = 1.2f, EyeForward = 0.5f });

            Vector3 flat = rule.Position(new Vector3(10f, 0f, 20f), Quaternion.identity);
            Assert.AreEqual(new Vector3(10f, 1.2f, 20.5f), flat);

            // Turned a quarter to the right: "ahead" is now along +x.
            Vector3 turned = rule.Position(new Vector3(10f, 0f, 20f), Quaternion.Euler(0f, 90f, 0f));
            Assert.AreEqual(10.5f, turned.x, Tolerance);
            Assert.AreEqual(1.2f, turned.y, Tolerance);
            Assert.AreEqual(20f, turned.z, Tolerance);
        }

        [Test]
        public void TheEyeLeansWithTheBody()
        {
            var rule = new FirstPersonCameraRule(new FirstPersonCameraSettings { EyeHeight = 1f, EyeForward = 0f });

            // Rolled 90° about the forward axis, "up" is along -x (a roll of +z turns the head to the left).
            Vector3 leaning = rule.Position(Vector3.zero, Quaternion.Euler(0f, 0f, 90f));

            Assert.AreEqual(-1f, leaning.x, Tolerance);
            Assert.AreEqual(0f, leaning.y, Tolerance);
        }

        [Test]
        public void TheCameraLooksDownALittleFromTheBodysDirection()
        {
            var rule = new FirstPersonCameraRule(new FirstPersonCameraSettings { PitchDownDegrees = 4f });

            Vector3 forward = rule.Rotation(Quaternion.identity) * Vector3.forward;

            Assert.Less(forward.y, 0f, "Pointing down.");
            Assert.AreEqual(4f, Vector3.Angle(Vector3.forward, forward), 1e-3f);
        }

        [Test]
        public void TheFieldOfViewGrowsWithSpeedAndStaysWithinItsLimits()
        {
            var settings = new FirstPersonCameraSettings { BaseFov = 70f, MaxFov = 80f, FovMinSpeed = 10f, FovMaxSpeed = 30f };
            var rule = new FirstPersonCameraRule(settings);

            Assert.AreEqual(70f, rule.Fov(0f), Tolerance, "Below the slow speed.");
            Assert.AreEqual(75f, rule.Fov(20f), Tolerance, "Half way.");
            Assert.AreEqual(80f, rule.Fov(60f), Tolerance, "Above the fast speed.");
        }

        [Test]
        public void ASettingOutOfRangeIsRefused()
        {
            Assert.Throws<ArgumentException>(() => new FirstPersonCameraRule(new FirstPersonCameraSettings { EyeHeight = -1f }));
            Assert.Throws<ArgumentException>(() => new FirstPersonCameraRule(new FirstPersonCameraSettings { BaseFov = 90f, MaxFov = 80f }));
            Assert.Throws<ArgumentException>(() => new FirstPersonCameraRule(new FirstPersonCameraSettings { FovMinSpeed = 30f, FovMaxSpeed = 30f }));
            Assert.Throws<ArgumentException>(() => new FirstPersonCameraRule(new FirstPersonCameraSettings { PitchDownDegrees = 90f }));
        }
    }
}
