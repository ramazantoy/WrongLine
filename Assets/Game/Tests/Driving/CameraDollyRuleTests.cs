using System;
using NUnit.Framework;
using UnityEngine;

namespace WrongLine.Driving.Tests
{
    public class CameraDollyRuleTests
    {
        private ChaseCameraSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new ChaseCameraSettings();
        }

        [Test]
        public void ItStartsTheNominalDistanceBehindTheSubject()
        {
            var rule = new CameraDollyRule(_settings);

            rule.Update(100.0, 0.016f);

            Assert.AreEqual(100.0 - _settings.Distance, rule.Distance, 1e-9);
            Assert.AreEqual(_settings.Distance, rule.Behind, 1e-9);
        }

        [Test]
        public void ItKeepsPaceWithASteadySpeedExactly_WhateverTheFrameTimes()
        {
            var rule = new CameraDollyRule(_settings);
            var random = new System.Random(7);
            double subject = 0.0;
            rule.Update(subject, 0.016f);

            for (int i = 0; i < 2000; i++)
            {
                // Frame times all over the place at 30 m/s: the camera never lags, never slides.
                float dt = 0.005f + (float)random.NextDouble() * 0.1f;
                subject += 30.0 * dt;
                rule.Update(subject, dt);

                Assert.AreEqual(_settings.Distance, rule.Behind, 1e-6, $"frame {i}");
            }
        }

        [Test]
        public void ALaunchLetsTheCarPullAheadButNeverOutOfRange_AndTheCameraComesBackToTheNominalDistance()
        {
            var rule = new CameraDollyRule(_settings);
            double subject = 0.0;
            rule.Update(subject, 1f / 60f);
            double worst = 0.0;

            for (int i = 0; i < 60 * 30; i++)
            {
                double t = i / 60.0;
                double speed = Math.Min(12.0 + 8.0 * t, 30.0); // the car's launch: 12 to 30 m/s at 8 m/s²
                subject += speed / 60.0;
                rule.Update(subject, 1f / 60f);

                // The range is a smooth limit, not a wall: the launch's lag is pulled in at the sharpness, so it can pass the edge a little.
                Assert.GreaterOrEqual(rule.Behind, _settings.RangeMin - 1e-6);
                Assert.LessOrEqual(rule.Behind, _settings.RangeMax + 0.8);
                worst = Math.Max(worst, rule.Behind);
            }

            Assert.Greater(worst, _settings.Distance + 1.0, "the car does pull away from the camera while it accelerates");
            Assert.AreEqual(_settings.Distance, rule.Behind, 0.05, "and the camera is back at the nominal distance 30 s later");
        }

        [Test]
        public void ASubjectThrownAheadIsFollowedToTheEdgeOfTheRangeAndThenEasedBack_WithoutOvershooting()
        {
            var rule = new CameraDollyRule(_settings);
            double subject = 100.0;
            rule.Update(subject, 1f / 60f);

            subject += 30.0; // teleport: 30 m in one frame
            rule.Update(subject, 1f / 60f);
            Assert.Greater(rule.Behind, _settings.RangeMax);

            double last = rule.Behind;
            for (int i = 0; i < 60 * 25; i++)
            {
                rule.Update(subject, 1f / 60f);
                Assert.LessOrEqual(rule.Behind, last + 1e-9, "it only closes in");
                Assert.GreaterOrEqual(rule.Behind, _settings.Distance - 1e-6, "never past the nominal distance");
                last = rule.Behind;
            }

            Assert.AreEqual(_settings.Distance, rule.Behind, 0.01);
        }

        [Test]
        public void ASubjectThrownBackEndsUpBackAtTheNominalDistanceFromTheNearSide()
        {
            var rule = new CameraDollyRule(_settings);
            rule.Update(100.0, 1f / 60f);

            rule.Update(80.0, 1f / 60f); // 20 m back in one frame: the subject is now behind the camera

            double last = rule.Behind;
            for (int i = 0; i < 60 * 25; i++)
            {
                rule.Update(80.0, 1f / 60f);
                Assert.GreaterOrEqual(rule.Behind, last - 1e-9, "it only moves away from the subject");
                Assert.LessOrEqual(rule.Behind, _settings.Distance + 1e-6);
                last = rule.Behind;
            }

            Assert.AreEqual(_settings.Distance, rule.Behind, 0.01);
        }

        [Test]
        public void WithoutTheWeakPullTheRangeAloneSettlesAThrownSubjectAtItsEdge()
        {
            _settings.RestoreSharpness = 0f;

            var ahead = new CameraDollyRule(_settings);
            ahead.Update(100.0, 1f / 60f);
            ahead.Update(130.0, 1f / 60f);
            var back = new CameraDollyRule(_settings);
            back.Update(100.0, 1f / 60f);
            back.Update(80.0, 1f / 60f);

            for (int i = 0; i < 60 * 10; i++)
            {
                ahead.Update(130.0, 1f / 60f);
                back.Update(80.0, 1f / 60f);
            }

            Assert.AreEqual(_settings.RangeMax, ahead.Behind, 1e-3);
            Assert.AreEqual(_settings.RangeMin, back.Behind, 1e-3);
        }

        [Test]
        public void AJoltOfTheSubjectInsideTheRangeIsNotCopiedByTheCamera()
        {
            var rule = new CameraDollyRule(_settings);
            double subject = 0.0;
            rule.Update(subject, 1f / 60f);
            for (int i = 0; i < 120; i++)
            {
                subject += 0.5;
                rule.Update(subject, 1f / 60f);
            }

            double before = rule.Distance;
            subject += 1.5; // a 1.5 m jump in one frame (90 m/s): inside the range, a jolt, not a speed the camera should take up whole
            rule.Update(subject, 1f / 60f);

            Assert.Less(rule.Distance - before, 1.0, "the camera moved less than half of what the subject jumped");
            Assert.LessOrEqual(rule.Behind, _settings.RangeMax);
        }

        [Test]
        public void TheSlideDoesNotDependOnTheFrameRate()
        {
            var fast = new CameraDollyRule(_settings);
            var slow = new CameraDollyRule(_settings);
            fast.Update(100.0, 0.016f);
            slow.Update(100.0, 0.016f);
            fast.Update(140.0, 0.016f);
            slow.Update(140.0, 0.016f);

            for (int i = 0; i < 60; i++)
            {
                fast.Update(140.0, 1f / 60f);
            }

            for (int i = 0; i < 6; i++)
            {
                slow.Update(140.0, 1f / 6f);
            }

            // The two slides (back into range, and back to the nominal distance) are applied one after the other each frame, so the
            // frame rate changes the result a little (a few percent of the 32 m slide); a per-frame constant would be off by several times that.
            Assert.AreEqual(fast.Distance, slow.Distance, 0.6);
        }

        [Test]
        public void APausedFrameLeavesTheCameraWhereItIs()
        {
            var rule = new CameraDollyRule(_settings);
            rule.Update(100.0, 0.016f);
            rule.Update(140.0, 0.016f);
            double before = rule.Distance;

            for (int i = 0; i < 100; i++)
            {
                rule.Update(140.0, 0f);
            }

            Assert.AreEqual(before, rule.Distance, 1e-9);
        }

        [Test]
        public void InABendTheCameraBacksOffByThePullBackAndComesForwardAgainOnTheStraight()
        {
            var rule = new CameraDollyRule(_settings);
            double subject = 0.0;
            rule.Update(subject, 1f / 60f, 0f);

            for (int i = 0; i < 60 * 15; i++)
            {
                subject += 30.0 / 60.0;
                rule.Update(subject, 1f / 60f, 1f);
            }

            Assert.AreEqual(_settings.Distance + _settings.BendPullback, rule.Behind, 0.1, "backed off by the whole pull-back in a sharp bend");

            for (int i = 0; i < 60 * 15; i++)
            {
                subject += 30.0 / 60.0;
                rule.Update(subject, 1f / 60f, 0f);
            }

            Assert.AreEqual(_settings.Distance, rule.Behind, 0.1, "and forward again on the straight");
        }

        [Test]
        public void TheBendIsTheTurnBetweenTheCameraAndTheLookPointAsAShareOfTheFullPullBackAngle()
        {
            Quaternion straight = Quaternion.identity;

            Assert.AreEqual(0f, CameraDollyRule.Bend(straight, straight, _settings), 1e-5f);
            Assert.AreEqual(0.5f, CameraDollyRule.Bend(straight, Quaternion.Euler(0f, _settings.BendPullbackDegrees * 0.5f, 0f), _settings), 1e-4f);
            Assert.AreEqual(1f, CameraDollyRule.Bend(straight, Quaternion.Euler(0f, _settings.BendPullbackDegrees * 3f, 0f), _settings), 1e-5f, "never above 1");
            Assert.AreEqual(0f, CameraDollyRule.Bend(straight, Quaternion.Euler(30f, 0f, 0f), _settings), 1e-4f, "a slope is not a bend: only the turn about up counts");
        }

        [Test]
        public void ARangeThatLeavesOutTheNominalDistanceIsRejected()
        {
            _settings.RangeMax = _settings.Distance - 1f;

            Assert.Throws<ArgumentException>(() => new CameraDollyRule(_settings));
        }
    }
}
