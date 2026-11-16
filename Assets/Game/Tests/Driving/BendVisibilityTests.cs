using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WrongLine.Road;
using WrongLine.Road.Tests;

namespace WrongLine.Driving.Tests
{
    /// <summary>The reason the camera looks along the road: at the entry of a sharp bend the bend's exit is already in the picture.</summary>
    public class BendVisibilityTests
    {
        private const float Aspect = 1080f / 1920f; // Portrait, the game's orientation.
        private const float Speed = 30f;

        private static float HorizontalHalfFov(float verticalFov)
        {
            return Mathf.Atan(Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad) * Aspect) * Mathf.Rad2Deg;
        }

        private static List<(double entry, PlacedPiece piece)> SharpBends(IRoadPath road)
        {
            var bends = new List<(double, PlacedPiece)>();
            for (int index = road.FirstIndex + 1; index < road.EndIndex; index++)
            {
                PlacedPiece piece = road[index];
                if (piece.Piece.Name.StartsWith("CurveSharp", StringComparison.Ordinal) || piece.Piece.Name.StartsWith("BankSharp", StringComparison.Ordinal))
                {
                    bends.Add((piece.EntryDistance, piece));
                }
            }

            return bends;
        }

        /// <summary>The angle, to the side, between where the camera points and the exit of the piece, at the moment the car enters it.</summary>
        private static float ExitAngle(IRoadPath road, PlacedPiece piece, Quaternion? lookStraightOn)
        {
            var settings = new ChaseCameraSettings();
            var rule = new ChaseCameraRule(settings);
            rule.Update(ChaseTarget.At(road, default, piece.EntryDistance, 0f, Speed, settings), 0.016f);

            Quaternion rotation = lookStraightOn.HasValue ? lookStraightOn.Value : rule.Rotation;
            Vector3 exit = RoadSampler.At(road, piece.ExitDistance).Position.RelativeTo(default);
            Vector3 inCamera = Quaternion.Inverse(rotation) * (exit - rule.Position);
            return Mathf.Abs(Mathf.Atan2(inCamera.x, inCamera.z) * Mathf.Rad2Deg);
        }

        [TestCase(1UL)]
        [TestCase(2UL)]
        [TestCase(3UL)]
        public void AtTheEntryOfASharpBend_ItsExitIsInThePicture(ulong seed)
        {
            RoadLayout road = RoadTestPieces.Layout(seed);
            road.EnsureCovered(5000.0);
            var settings = new ChaseCameraSettings();
            float halfFov = HorizontalHalfFov(settings.MaxFov);

            List<(double entry, PlacedPiece piece)> bends = SharpBends(road);
            Assert.GreaterOrEqual(bends.Count, 1, "The seed should contain at least one sharp bend for this to mean anything.");
            foreach ((double _, PlacedPiece piece) in bends)
            {
                Assert.Less(ExitAngle(road, piece, null), halfFov * 0.95f, $"{piece.Piece.Name} at {piece.EntryDistance:F0} m");
            }
        }

        [TestCase(1UL)]
        [TestCase(2UL)]
        [TestCase(3UL)]
        public void AlongTheWholeRoad_TheCarNeverLeavesThePicture(ulong seed)
        {
            RoadLayout road = RoadTestPieces.Layout(seed);
            road.EnsureCovered(4500.0);
            var settings = new ChaseCameraSettings();
            var rule = new ChaseCameraRule(settings);
            float halfHorizontal = HorizontalHalfFov(settings.MaxFov);
            float halfVertical = settings.MaxFov * 0.5f;

            float worstHorizontal = 0f;
            float worstVertical = 0f;
            string where = "";
            for (double s = 20.0; s < 4000.0; s += 1.0)
            {
                ChaseTarget target = ChaseTarget.At(road, default, s, 0f, Speed, settings);
                rule.Update(target, 0.016f);
                Vector3 inCamera = Quaternion.Inverse(rule.Rotation) * (target.CarPosition - rule.Position);
                float horizontal = Mathf.Abs(Mathf.Atan2(inCamera.x, inCamera.z) * Mathf.Rad2Deg);
                float vertical = Mathf.Abs(Mathf.Atan2(inCamera.y, inCamera.z) * Mathf.Rad2Deg);
                if (horizontal / halfHorizontal + vertical / halfVertical > worstHorizontal / halfHorizontal + worstVertical / halfVertical)
                {
                    where = $"at {s:F0} m";
                }

                worstHorizontal = Mathf.Max(worstHorizontal, horizontal);
                worstVertical = Mathf.Max(worstVertical, vertical);
            }

            Assert.Less(worstHorizontal, halfHorizontal * 0.95f, $"sideways, worst {where}");
            Assert.Less(worstVertical, halfVertical * 0.9f, $"up and down, worst {where}");
        }

        [Test]
        public void LookingStraightOn_TheSameExitsAreOutOfThePicture()
        {
            // The control: with the camera along the car's heading (how it used to look) the exits fall outside, so the check above says something.
            RoadLayout road = RoadTestPieces.Layout(1);
            road.EnsureCovered(5000.0);
            float halfFov = HorizontalHalfFov(new ChaseCameraSettings().MaxFov);

            int outside = 0;
            foreach ((double _, PlacedPiece piece) in SharpBends(road))
            {
                Quaternion heading = RoadSampler.At(road, piece.EntryDistance).Rotation;
                if (ExitAngle(road, piece, heading) > halfFov)
                {
                    outside++;
                }
            }

            Assert.Greater(outside, 0);
        }
    }
}
