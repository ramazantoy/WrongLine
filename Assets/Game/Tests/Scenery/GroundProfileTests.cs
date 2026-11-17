using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using WrongLine.Road;
using WrongLine.Road.Tests;

namespace WrongLine.Scenery.Tests
{
    public class GroundProfileTests
    {
        private static readonly GroundSettings _settings = new GroundSettings(0.3f, 80f, 0.8f, 20f, 2f);

        private static float MaxCurvature(TrackShape shape, int side)
        {
            float tightest = 0f;
            for (float s = 0f; s <= shape.Length; s += 0.5f)
            {
                tightest = Mathf.Max(tightest, GroundProfile.Curvature(shape, s) * side);
            }

            return tightest;
        }

        [Test]
        public void TheGround_StartsAtTheCurbsEdgeBelowTheAsphalt_AndRunsOutLevel()
        {
            var frame = new TrackFrame(new Vector3(0f, 5f, 0f), Quaternion.identity);

            Vector3 edge = GroundProfile.Point(frame, GroundProfile.EdgeX, 0.3f);
            Vector3 far = GroundProfile.Point(frame, 40f, 0.3f);
            Vector3 left = GroundProfile.Point(frame, -40f, 0.3f);

            Assert.AreEqual(new Vector3(GroundProfile.EdgeX, 4.7f, 0f).ToString("F4"), edge.ToString("F4"));
            Assert.AreEqual(new Vector3(40f, 4.7f, 0f).ToString("F4"), far.ToString("F4"));
            Assert.AreEqual(new Vector3(-40f, 4.7f, 0f).ToString("F4"), left.ToString("F4"));
            Assert.AreEqual(edge.ToString("F4"), GroundProfile.Point(frame, 1f, 0.3f).ToString("F4"), "Nearer than the edge gives the edge.");
        }

        [Test]
        public void OnABank_TheGroundMeetsTheTiltedRoad_ButRunsOutLevel()
        {
            Quaternion banked = Quaternion.Euler(0f, 30f, 0f) * Quaternion.Euler(0f, 0f, -12f);
            var frame = new TrackFrame(Vector3.zero, banked);

            Vector3 rightEdge = GroundProfile.Point(frame, GroundProfile.EdgeX, 0.3f);
            Vector3 leftEdge = GroundProfile.Point(frame, -GroundProfile.EdgeX, 0.3f);
            Vector3 rightFar = GroundProfile.Point(frame, 50f, 0.3f);

            Assert.Greater(Mathf.Abs(rightEdge.y - leftEdge.y), 2f, "The two edges sit at the heights of the banked road.");
            Assert.AreEqual(rightEdge.y, rightFar.y, 1e-4f, "Out from the edge the ground is level.");
            Vector3 outward = rightFar - rightEdge;
            Assert.AreEqual(0f, Vector3.Dot(outward.normalized, frame.Forward), 1e-4f, "It runs out at right angles to the road.");
        }

        [Test]
        public void FlatRight_IsLevelAndAtRightAnglesToTheHeading_EvenOnASteepSlope()
        {
            Quaternion steep = Quaternion.Euler(0f, -50f, 0f) * Quaternion.Euler(-65f, 0f, 0f);
            var frame = new TrackFrame(Vector3.zero, steep);

            Vector3 right = GroundProfile.FlatRight(frame);

            Assert.AreEqual(0f, right.y, 1e-6f);
            Assert.AreEqual(1f, right.magnitude, 1e-5f);
            Assert.AreEqual(0f, Vector3.Dot(right, frame.Forward), 1e-4f);
            Assert.Greater(Vector3.Dot(right, frame.Right), 0.99f, "Still the right-hand side.");
        }

        [Test]
        public void OnAStraight_TheGroundReachesItsFullWidthOnBothSides()
        {
            TrackShape straight = TrackIntegrator.Bake(TrackRecipe.Straight(40f));

            Assert.AreEqual(GroundProfile.EdgeX + 80f, GroundProfile.Reach(straight, 20f, 1, _settings), 1e-4f);
            Assert.AreEqual(GroundProfile.EdgeX + 80f, GroundProfile.Reach(straight, 20f, -1, _settings), 1e-4f);
        }

        [Test]
        public void InsideEveryBend_TheGroundStopsShortOfTheBendsCentre_OutsideItRunsFull()
        {
            List<RoadPieceInfo> pieces = RoadTestPieces.All();
            int bends = 0;
            foreach (RoadPieceInfo piece in pieces)
            {
                foreach (int side in new[] { 1, -1 })
                {
                    float tightest = MaxCurvature(piece.Shape, side);
                    if (tightest < 1e-3f)
                    {
                        continue;
                    }

                    bends++;
                    for (float s = 0f; s <= piece.Length; s += 1f)
                    {
                        float reach = GroundProfile.Reach(piece.Shape, s, side, _settings);
                        float radius = 1f / Mathf.Max(1e-6f, GroundProfile.Curvature(piece.Shape, s) * side);
                        Assert.LessOrEqual(reach, Mathf.Max(GroundProfile.EdgeX, _settings.InnerReachRatio * radius) + 1e-3f, $"{piece.Name} side {side} at {s}");
                        Assert.GreaterOrEqual(reach, GroundProfile.EdgeX);
                    }
                }
            }

            Assert.Greater(bends, 10, "The default pieces have bends both ways.");

            RoadPieceInfo right = pieces.First(piece => piece.Name.StartsWith("CurveSharp") && piece.Name.EndsWith("_R"));
            float middle = right.Length * 0.5f;
            Assert.Less(GroundProfile.Reach(right.Shape, middle, 1, _settings), GroundProfile.EdgeX + 80f, "Inside (right) is cut.");
            Assert.AreEqual(GroundProfile.EdgeX + 80f, GroundProfile.Reach(right.Shape, middle, -1, _settings), 1e-4f, "Outside runs full.");
        }

        [Test]
        public void Curvature_IsPositiveForARightBend_AndMatchesItsTurn()
        {
            TrackShape shape = TrackIntegrator.Bake(new TrackRecipe(60f, 40f, YawProfile.Single, 0f, PitchProfile.Bump, 0f));

            float turned = 0f;
            for (float s = 0f; s < shape.Length; s += 0.5f)
            {
                float k = GroundProfile.Curvature(shape, s + 0.25f);
                Assert.GreaterOrEqual(k, -1e-4f);
                turned += k * 0.5f;
            }

            Assert.AreEqual(40f, turned * Mathf.Rad2Deg, 2f, "Summed along the piece, the curvature is the piece's turn.");
            Assert.AreEqual(0f, GroundProfile.Curvature(TrackIntegrator.Bake(TrackRecipe.Straight(30f)), 15f), 1e-5f);
        }

        [Test]
        public void OnAHill_TheGroundStartsAtTheRoad_AndSettlesToTheLineBetweenThePiecesEnds_NoFloatingTerrace()
        {
            RoadPieceInfo hill = RoadTestPieces.Named(RoadTestPieces.All(), "Hill_L");
            TrackShape shape = hill.Shape;
            float crest = 0f;
            float crestS = 0f;
            for (float s = 0f; s <= shape.Length; s += 1f)
            {
                if (shape.Evaluate(s).Position.y > crest)
                {
                    crest = shape.Evaluate(s).Position.y;
                    crestS = s;
                }
            }

            Assume.That(crest, Is.GreaterThan(2f), "Hill_L rises.");
            float reach = GroundProfile.Reach(shape, crestS, 1, _settings);

            Vector3 atCurb = GroundProfile.Ground(shape, crestS, GroundProfile.EdgeX, reach, 0.3f);
            Vector3 middle = GroundProfile.Ground(shape, crestS, (GroundProfile.EdgeX + reach) * 0.5f, reach, 0.3f);
            Vector3 farEdge = GroundProfile.Ground(shape, crestS, reach, reach, 0.3f);

            Assert.AreEqual(crest - 0.3f, atCurb.y, 0.05f, "Meets the road.");
            Assert.AreEqual(GroundProfile.Baseline(shape, crestS) - 0.3f, farEdge.y, 1e-3f, "Settles to the line between the ends.");
            Assert.Less(farEdge.y, middle.y);
            Assert.Less(middle.y, atCurb.y, "It slopes down from the road: a ridge, not a terrace.");
        }

        [Test]
        public void OnAFlatPiece_SettlingChangesNothing()
        {
            TrackShape straight = TrackIntegrator.Bake(TrackRecipe.Straight(40f));
            var frame = straight.Evaluate(15f);

            Vector3 ground = GroundProfile.Ground(straight, 15f, 30f, GroundProfile.EdgeX + 80f, 0.3f);

            Assert.AreEqual(GroundProfile.Point(frame, 30f, 0.3f).ToString("F4"), ground.ToString("F4"));
        }

        [Test]
        public void NonsenseGroundSettings_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new GroundSettings(0.3f, 0f, 0.8f, 20f, 2f).Validate());
            Assert.Throws<ArgumentException>(() => new GroundSettings(0.3f, 80f, 2f, 20f, 2f).Validate());
            Assert.Throws<ArgumentException>(() => new GroundSettings(-1f, 80f, 0.8f, 20f, 2f).Validate());
            Assert.Throws<ArgumentException>(() => new GroundSettings(0.3f, 80f, 0.8f, 20f, 0f).Validate());
        }
    }
}
