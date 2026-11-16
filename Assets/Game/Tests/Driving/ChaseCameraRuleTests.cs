using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using WrongLine.Road;
using WrongLine.Road.Tests;

namespace WrongLine.Driving.Tests
{
    public class ChaseCameraRuleTests
    {
        private static ChaseTarget TargetAt(IRoadPath path, double distance, ChaseCameraSettings settings, float carX = 0f, float speed = 30f)
        {
            return ChaseTarget.At(path, default, distance, carX, speed, settings);
        }

        [Test]
        public void OnAStraight_TheCameraSitsBehindAndAboveTheCar_LookingAhead()
        {
            var settings = new ChaseCameraSettings();
            var rule = new ChaseCameraRule(settings);
            RoadLayout road = RoadTestPieces.Layout(1);

            rule.Update(TargetAt(road, 30.0, settings), 0.016f);

            Assert.AreEqual(0f, Vector3.Distance(new Vector3(0f, settings.Height, 30f - settings.Distance), rule.Position), 1e-4f);
            Vector3 toLookAt = new Vector3(0f, 0f, 30f + settings.LookAhead) - rule.Position;
            Assert.Less(Vector3.Angle(toLookAt, rule.Rotation * Vector3.forward), 0.01f);
        }

        [Test]
        public void TheSameRoadPlace_GivesTheSamePicture_WhateverTheFrameTimesWere()
        {
            var settings = new ChaseCameraSettings();
            RoadLayout road = RoadTestPieces.Layout(4);
            road.EnsureCovered(1500.0);
            float[] pattern = { 1f / 60f, 1f / 30f, 1f / 60f, 0.12f, 1f / 144f, 1f / 20f };

            var steady = new ChaseCameraRule(settings);
            var uneven = new ChaseCameraRule(settings);
            double s = 0.0;
            for (int i = 0; i < 600; i++)
            {
                s += 30.0 * (1f / 60f);
                steady.Update(TargetAt(road, s, settings), 1f / 60f);
                uneven.Update(TargetAt(road, s, settings), pattern[i % pattern.Length]);

                Assert.AreEqual(0f, Vector3.Distance(steady.Position, uneven.Position), 1e-4f, $"position at step {i}");
                Assert.Less(Quaternion.Angle(steady.Rotation, uneven.Rotation), 0.01f, $"rotation at step {i}");
            }
        }

        [Test]
        public void ACarDrivingAStraight_SitsStillInThePicture_EvenWithUnevenFrames()
        {
            var settings = new ChaseCameraSettings();
            RoadLayout road = RoadTestPieces.Layout(1);
            var rule = new ChaseCameraRule(settings);
            float[] pattern = { 1f / 60f, 1f / 30f, 1f / 60f, 0.12f, 1f / 144f };

            double s = settings.Distance + 1.0;
            float minimum = float.MaxValue;
            float maximum = float.MinValue;
            for (int i = 0; i < 200 && s + settings.LookAhead + 4.0 < road[0].Piece.Length; i++)
            {
                float deltaTime = pattern[i % pattern.Length];
                s += 30.0 * deltaTime;
                rule.Update(TargetAt(road, s, settings), deltaTime);

                RoadSample car = RoadSampler.At(road, s);
                Vector3 inCamera = Quaternion.Inverse(rule.Rotation) * (car.Position.RelativeTo(default) - rule.Position);
                minimum = Mathf.Min(minimum, inCamera.z);
                maximum = Mathf.Max(maximum, inCamera.z);
            }

            Assert.Less(maximum - minimum, 1e-3f, "The car's distance in the picture does not wobble with the frame time.");
        }

        [Test]
        public void IntoABend_TheCameraLooksAlongTheRoadAheadInsteadOfStraightOn()
        {
            var settings = new ChaseCameraSettings();
            List<RoadPieceInfo> all = RoadTestPieces.All();
            RoadPieceInfo bend = all.First(piece => piece.Name == "CurveSoft_R");
            RoadLayout road = RoadTestPieces.Layout(1, null, new List<RoadPieceInfo> { RoadTestPieces.Start(all), bend });
            road.EnsureCovered(road[0].Piece.Length + 1.0);
            double distance = road[1].EntryDistance + 10.0;
            var rule = new ChaseCameraRule(settings);

            rule.Update(TargetAt(road, distance, settings), 0.016f);

            Vector3 carForward = RoadSampler.At(road, distance).Rotation * Vector3.forward;
            float lead = Vector3.SignedAngle(carForward, rule.Rotation * Vector3.forward, Vector3.up);
            Assert.Greater(lead, 2f, "A right bend: the camera already points to the right of where the car points.");
        }

        [Test]
        public void TheCameraFollowsTheCarSideways_AtTheSameSpeedWhateverTheFrameRate()
        {
            var settings = new ChaseCameraSettings();
            RoadLayout road = RoadTestPieces.Layout(1);
            float[] results = new float[3];
            float[] deltaTimes = { 1f / 120f, 1f / 60f, 1f / 15f };

            for (int run = 0; run < deltaTimes.Length; run++)
            {
                var rule = new ChaseCameraRule(settings);
                rule.Update(TargetAt(road, 30.0, settings, 0f), 0.016f);
                int steps = Mathf.RoundToInt(0.2f / deltaTimes[run]);
                for (int i = 0; i < steps; i++)
                {
                    rule.Update(TargetAt(road, 30.0, settings, 2f), deltaTimes[run]);
                }

                results[run] = rule.Position.x;
            }

            float expected = 2f * settings.FollowX * (1f - Mathf.Exp(-settings.LateralSharpness * 0.2f));
            foreach (float result in results)
            {
                Assert.AreEqual(expected, result, 0.01f);
            }
        }

        [Test]
        public void TheCameraMovesOver_ForACarOnTheRight()
        {
            var settings = new ChaseCameraSettings();
            RoadLayout road = RoadTestPieces.Layout(1);
            var rule = new ChaseCameraRule(settings);

            rule.Update(TargetAt(road, 30.0, settings, 3f), 0.016f);

            Assert.AreEqual(3f * settings.FollowX, rule.Position.x, 1e-4f);
        }

        [Test]
        public void APause_LeavesTheSidewaysPositionAlone()
        {
            var settings = new ChaseCameraSettings();
            RoadLayout road = RoadTestPieces.Layout(1);
            var rule = new ChaseCameraRule(settings);
            rule.Update(TargetAt(road, 30.0, settings, 0f), 0.016f);

            for (int i = 0; i < 50; i++)
            {
                rule.Update(TargetAt(road, 30.0, settings, 3f), 0f);
            }

            Assert.AreEqual(0f, rule.Position.x, 1e-6f);
        }

        [TestCase("SteepDown_S")]
        [TestCase("SteepUp_S")]
        [TestCase("SteepUp_L")]
        public void OnASteepSlope_TheCarStaysInThePicture(string pieceName)
        {
            var settings = new ChaseCameraSettings();
            List<RoadPieceInfo> all = RoadTestPieces.All();
            RoadPieceInfo steep = all.First(piece => piece.Name == pieceName);
            RoadLayout road = RoadTestPieces.Layout(1, null, new List<RoadPieceInfo> { RoadTestPieces.Start(all), steep, RoadTestPieces.Named(all, "Straight_XL") });
            road.EnsureCovered(road[0].Piece.Length + 600.0);
            PlacedPiece piece = default;
            bool found = false;
            for (int index = road.FirstIndex; index < road.EndIndex && !found; index++)
            {
                if (road[index].Piece.Name == pieceName)
                {
                    piece = road[index];
                    found = true;
                }
            }

            Assert.IsTrue(found, "The generator should have placed the piece within 600 m.");
            var rule = new ChaseCameraRule(settings);

            float worst = 0f;
            for (double s = piece.EntryDistance - 15.0; s < piece.ExitDistance + 15.0; s += 1.0)
            {
                ChaseTarget target = TargetAt(road, s, settings);
                rule.Update(target, 0.016f);
                Vector3 toCar = Quaternion.Inverse(rule.Rotation) * (target.CarPosition - rule.Position);
                worst = Mathf.Max(worst, Mathf.Abs(Mathf.Atan2(toCar.y, toCar.z) * Mathf.Rad2Deg));
            }

            Assert.Less(worst, settings.BaseFov * 0.5f * 0.9f, "The car's angle from the middle of the picture, worst over the slope.");
        }

        [Test]
        public void TheFieldOfView_WidensWithSpeed_AndStaysWithinItsLimits()
        {
            var settings = new ChaseCameraSettings();
            RoadLayout road = RoadTestPieces.Layout(1);
            var rule = new ChaseCameraRule(settings);

            rule.Update(TargetAt(road, 30.0, settings, 0f, 0f), 0.016f);
            Assert.AreEqual(settings.BaseFov, rule.Fov, 1e-4f);

            float last = rule.Fov;
            for (float speed = 0f; speed <= 40f; speed += 1f)
            {
                rule.Update(TargetAt(road, 30.0, settings, 0f, speed), 0.016f);
                Assert.GreaterOrEqual(rule.Fov, last - 1e-4f, $"speed {speed}");
                Assert.That(rule.Fov, Is.InRange(settings.BaseFov - 1e-4f, settings.MaxFov + 1e-4f));
                last = rule.Fov;
            }

            Assert.AreEqual(settings.MaxFov, rule.Fov, 1e-4f);
        }
    }
}
