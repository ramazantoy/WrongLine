using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace WrongLine.Road.Tests
{
    public class OriginShiftTests
    {
        [Test]
        public void WithinTheThreshold_NothingMoves_IncludingExactlyOnIt()
        {
            var shift = new OriginShift(500.0);

            Assert.IsFalse(shift.Update(new WorldPoint(300, 0, 400), out Vector3 moved));
            Assert.AreEqual(Vector3.zero, moved);
            Assert.AreEqual(0.0, shift.Origin.X);
        }

        [Test]
        public void BeyondTheThreshold_TheOriginMovesToTheFollower()
        {
            var shift = new OriginShift(500.0);
            var follower = new WorldPoint(300, 10, 500);

            Assert.IsTrue(shift.Update(follower, out Vector3 moved));

            Assert.AreEqual(follower.X, shift.Origin.X);
            Assert.AreEqual(follower.Z, shift.Origin.Z);
            Assert.AreEqual(0f, Vector3.Distance(new Vector3(300f, 10f, 500f), moved), 1e-4f);
            Assert.AreEqual(Vector3.zero, follower.RelativeTo(shift.Origin), "The follower is at the scene origin afterwards.");
            Assert.IsFalse(shift.Update(follower, out _));
        }

        [Test]
        public void ThresholdMustBePositive()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new OriginShift(0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new OriginShift(-1.0));
        }

        [Test]
        public void FarOut_PiecesStillMeetInTheSceneWhenPlacedFromTheDoubles_ButNotWithoutAnOrigin()
        {
            RoadLayout layout = RoadTestPieces.Layout(7);
            layout.EnsureCovered(1_100_000.0);
            int index = layout.FirstIndex;
            while (layout[index].EntryDistance < 1_000_000.0)
            {
                index++;
            }

            WorldPoint origin = layout[index].Entry.Position;
            double withOrigin = 0.0;
            double withoutOrigin = 0.0;
            for (int i = index; i < index + 100; i++)
            {
                withOrigin = Math.Max(withOrigin, SeamGap(layout[i], layout[i + 1], origin));
                withoutOrigin = Math.Max(withoutOrigin, SeamGap(layout[i], layout[i + 1], new WorldPoint(0, 0, 0)));
            }

            Assert.Less(withOrigin, 1e-3, "With the origin near, a seam is closed to a millimetre.");
            Assert.Greater(withoutOrigin, 1e-2, "Without it, float positions a thousand kilometres out would tear the seams open.");
        }

        [Test]
        public void DrivingOneHundredKilometres_KeepsTheFollowerNearTheSceneOrigin()
        {
            const double Threshold = 500.0;
            const double Step = 3.0;
            RoadLayout layout = RoadTestPieces.Layout(8);
            var cursor = new RoadCursor(layout);
            var window = new RoadWindow(layout, 300.0, 60.0);
            var shift = new OriginShift(Threshold);
            var spawned = new List<int>();
            var despawned = new List<int>();
            int shifts = 0;
            float farthest = 0f;

            for (double driven = 0.0; driven < 100_000.0; driven += Step)
            {
                window.Update(cursor, spawned, despawned);
                layout.DiscardBefore(window.First);
                if (shift.Update(cursor.WorldPosition, out _))
                {
                    shifts++;
                }

                farthest = Mathf.Max(farthest, cursor.WorldPosition.RelativeTo(shift.Origin).magnitude);
                cursor.Advance(Step);
            }

            Assert.LessOrEqual(farthest, Threshold + 1e-3, "Never farther from the scene origin than the threshold.");
            Assert.That(shifts, Is.InRange(40, 220), "The road moves forward at least a quarter of the distance driven (yaw band), at most all of it.");
        }

        // How far apart the end of one piece and the start of the next land in the scene (floats), for a given origin.
        private static double SeamGap(PlacedPiece a, PlacedPiece b, WorldPoint origin)
        {
            Vector3 aEnd = RoadPlacement.Position(a, origin) + RoadPlacement.Rotation(a) * a.Piece.Shape.Exit.Position;
            Vector3 bStart = RoadPlacement.Position(b, origin) + RoadPlacement.Rotation(b) * b.Piece.Shape.Entry.Position;
            return Vector3.Distance(aEnd, bStart);
        }
    }
}
