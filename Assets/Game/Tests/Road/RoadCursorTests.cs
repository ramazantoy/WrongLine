using System;
using NUnit.Framework;
using UnityEngine;

namespace WrongLine.Road.Tests
{
    public class RoadCursorTests
    {
        [Test]
        public void StartsAtTheStartOfTheFirstPiece()
        {
            RoadLayout layout = RoadTestPieces.Layout(1);
            var cursor = new RoadCursor(layout);

            Assert.AreEqual(0, cursor.PieceIndex);
            Assert.AreEqual(0.0, cursor.LocalS);
            Assert.AreEqual(0.0, cursor.Distance);
            Assert.AreEqual(0.0, cursor.WorldPosition.DistanceTo(new WorldPoint(0, 0, 0)), 1e-9);
        }

        [Test]
        public void Advance_WithinAPiece_MovesTheLocalDistance()
        {
            var cursor = new RoadCursor(RoadTestPieces.Layout(1));

            cursor.Advance(12.5);

            Assert.AreEqual(0, cursor.PieceIndex);
            Assert.AreEqual(12.5, cursor.LocalS, 1e-12);
            Assert.AreEqual(12.5, cursor.Distance, 1e-12);
        }

        [Test]
        public void ReachingAJointExactly_StepsOntoTheNextPiece_AndJustShortDoesNot()
        {
            RoadLayout layout = RoadTestPieces.Layout(1);
            double length = layout[0].Piece.Length;

            var shortOfIt = new RoadCursor(layout);
            shortOfIt.Advance(length - 0.001);
            Assert.AreEqual(0, shortOfIt.PieceIndex);

            var onIt = new RoadCursor(layout);
            onIt.Advance(length);
            Assert.AreEqual(1, onIt.PieceIndex);
            Assert.AreEqual(0.0, onIt.LocalS);
            Assert.AreEqual(length, onIt.Distance, 1e-9);
        }

        [Test]
        public void OneBigStep_AndManySmallOnes_EndAtTheSamePlace()
        {
            RoadLayout layout = RoadTestPieces.Layout(2);
            var big = new RoadCursor(layout);
            var small = new RoadCursor(layout);

            big.Advance(3333.3);
            for (int i = 0; i < 4000; i++)
            {
                small.Advance(3333.3 / 4000.0);
            }

            Assert.AreEqual(big.PieceIndex, small.PieceIndex);
            Assert.AreEqual(0.0, big.WorldPosition.DistanceTo(small.WorldPosition), 1e-4);
            Assert.AreEqual(3333.3, big.Distance, 1e-9);
        }

        [Test]
        public void RunningPastTheEnd_PlacesMoreRoad()
        {
            RoadLayout layout = RoadTestPieces.Layout(3);
            var cursor = new RoadCursor(layout);
            int before = layout.EndIndex;

            cursor.Advance(5000.0);

            Assert.Greater(layout.EndIndex, before);
            Assert.AreEqual(5000.0, cursor.Distance, 1e-6);
            Assert.GreaterOrEqual(layout.EndDistance, 5000.0);
        }

        [Test]
        public void WalkingOneMetreAtATime_TravelsOneMetreOfRealRoad()
        {
            RoadLayout layout = RoadTestPieces.Layout(4);
            var cursor = new RoadCursor(layout);
            WorldPoint previous = cursor.WorldPosition;

            for (int i = 0; i < 3000; i++)
            {
                cursor.Advance(1.0);
                WorldPoint now = cursor.WorldPosition;
                double step = previous.DistanceTo(now);

                // The chord of a one metre arc is a hair short on a bend (radius >= 8 m: at least 0.995) and never longer.
                Assert.That(step, Is.InRange(0.99, 1.0005), $"At {cursor.Distance:F0} m, piece {cursor.PieceIndex}.");
                previous = now;
            }
        }

        [Test]
        public void CrossingAJoint_IsContinuous()
        {
            RoadLayout layout = RoadTestPieces.Layout(5);
            layout.EnsureCovered(6000.0); // Enough for 30 pieces however long the generator makes them.

            for (int i = 1; i < 30; i++)
            {
                double joint = layout[i].EntryDistance;
                var before = new RoadCursor(layout);
                before.Advance(joint - 0.01);
                var after = new RoadCursor(layout);
                after.Advance(joint + 0.01);

                Assert.AreEqual(i - 1, before.PieceIndex);
                Assert.AreEqual(i, after.PieceIndex);
                Assert.AreEqual(0.02, before.WorldPosition.DistanceTo(after.WorldPosition), 2e-3, $"Joint {i}.");
            }
        }

        [Test]
        public void TheRotation_FacesTheWayTheCursorIsMoving_AndKeepsTheBankedUp()
        {
            RoadLayout layout = RoadTestPieces.Layout(6);
            var cursor = new RoadCursor(layout);

            for (int i = 0; i < 4000; i++)
            {
                WorldPoint a = cursor.WorldPosition;
                Quaternion rotation = cursor.WorldRotation;
                cursor.Advance(0.5);
                Vector3 chord = cursor.WorldPosition.RelativeTo(a);

                Assert.Less(Vector3.Angle(chord, rotation * Vector3.forward), 2.5f, $"At {cursor.Distance:F1} m.");
                Vector3 expectedUp = cursor.Piece.Entry.TransformDirection(cursor.LocalFrame.Up);
                Assert.Less(Vector3.Angle(expectedUp, cursor.WorldRotation * Vector3.up), 0.01f);
            }
        }

        [TestCase(-1.0)]
        [TestCase(double.NaN)]
        public void MovingBackwards_OrByNothingMeaningful_IsRejected(double meters)
        {
            var cursor = new RoadCursor(RoadTestPieces.Layout(1));

            Assert.Throws<ArgumentOutOfRangeException>(() => cursor.Advance(meters));
        }
    }
}
