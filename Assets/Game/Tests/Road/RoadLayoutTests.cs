using System;
using NUnit.Framework;
using UnityEngine;

namespace WrongLine.Road.Tests
{
    public class RoadLayoutTests
    {
        private const double JointTolerance = 1e-9;

        [Test]
        public void StartsWithTheStartPiece_AtTheOrigin()
        {
            RoadLayout layout = RoadTestPieces.Layout(1);

            Assert.AreEqual(1, layout.EndIndex);
            Assert.AreEqual(0, layout.FirstIndex);
            Assert.AreEqual(0, layout[0].Index);
            Assert.AreEqual(0.0, layout[0].EntryDistance);
            Assert.AreEqual(60.0, layout.EndDistance, 1e-9);
            Assert.AreEqual(1UL, layout.Seed);
        }

        [Test]
        public void EnsureCovered_ReachesTheDistance_AndDoesNothingWhenAlreadyThere()
        {
            RoadLayout layout = RoadTestPieces.Layout(2);

            layout.EnsureCovered(1000.0);
            int count = layout.EndIndex;

            Assert.GreaterOrEqual(layout.EndDistance, 1000.0);
            layout.EnsureCovered(900.0);
            Assert.AreEqual(count, layout.EndIndex);
        }

        [Test]
        public void Distances_AreContinuous_AndMatchThePieceLengths()
        {
            RoadLayout layout = RoadTestPieces.Layout(3);
            layout.EnsureCovered(3000.0);

            for (int i = 1; i < layout.EndIndex; i++)
            {
                Assert.AreEqual(layout[i - 1].ExitDistance, layout[i].EntryDistance, JointTolerance);
                Assert.AreEqual(layout[i].Piece.Length, layout[i].ExitDistance - layout[i].EntryDistance, 1e-9);
            }
        }

        [Test]
        public void EveryJoint_IsTheSamePlaceAndHeading_SeenFromBothPieces()
        {
            RoadLayout layout = RoadTestPieces.Layout(4);
            layout.EnsureCovered(20000.0);

            for (int i = 1; i < layout.EndIndex; i++)
            {
                PlacedPiece previous = layout[i - 1];
                PlacedPiece next = layout[i];

                WorldPoint end = previous.Entry.TransformPoint(previous.Piece.Shape.Exit.Position);
                WorldPoint start = next.Entry.TransformPoint(next.Piece.Shape.Entry.Position);
                Assert.AreEqual(0.0, end.DistanceTo(start), 1e-3, $"Joint {i}: position.");

                Vector3 outgoing = previous.Entry.TransformDirection(previous.Piece.Shape.Exit.Forward);
                Vector3 incoming = next.Entry.TransformDirection(next.Piece.Shape.Entry.Forward);
                Assert.Less(Vector3.Angle(outgoing, incoming), 0.01f, $"Joint {i}: heading.");
                Assert.AreEqual(Vector3.up, next.Entry.TransformDirection(next.Piece.Shape.Entry.Up));
            }
        }

        [Test]
        public void WorldFramesFollowTheirOwnHeading_SoTheTurnDirectionIsRight()
        {
            RoadLayout layout = RoadTestPieces.Layout(6);
            layout.EnsureCovered(4000.0);

            for (int i = 0; i < layout.EndIndex; i++)
            {
                PlacedPiece placed = layout[i];
                TrackShape shape = placed.Piece.Shape;
                for (int f = 0; f + 1 < shape.Count; f++)
                {
                    WorldPoint a = placed.Entry.TransformPoint(shape[f].Position);
                    WorldPoint b = placed.Entry.TransformPoint(shape[f + 1].Position);
                    var chord = new Vector3((float)(b.X - a.X), (float)(b.Y - a.Y), (float)(b.Z - a.Z));
                    Vector3 forward = placed.Entry.TransformDirection(shape[f].Forward);

                    Assert.Less(Vector3.Angle(chord, forward), 2.5f, $"Piece {i} {placed.Piece}, frame {f}.");
                }
            }
        }

        [Test]
        public void AFarPoint_KeepsItsPrecision_BecauseTheMathIsInDouble()
        {
            RoadLayout layout = RoadTestPieces.Layout(9);
            layout.EnsureCovered(2_000_000.0); // Two thousand kilometres.

            PlacedPiece far = layout[layout.EndIndex - 1];
            WorldPoint entry = far.Entry.Position;
            WorldPoint exit = far.Entry.TransformPoint(far.Piece.Shape.Exit.Position);

            Assert.Greater(far.EntryDistance, 1_999_000.0);
            Assert.AreEqual(far.Piece.ExitOffset.magnitude, (float)entry.DistanceTo(exit), 1e-3f, "A piece is the same size however far out it is.");
        }

        [Test]
        public void DiscardBefore_DropsOldPieces_AndKeepsTheRestAndCanKeepGrowing()
        {
            RoadLayout layout = RoadTestPieces.Layout(10);
            layout.EnsureCovered(2000.0);
            int end = layout.EndIndex;
            PlacedPiece kept = layout[end - 3];

            layout.DiscardBefore(end - 3);

            Assert.AreEqual(end - 3, layout.FirstIndex);
            Assert.AreEqual(end, layout.EndIndex);
            Assert.AreEqual(kept.Piece, layout[end - 3].Piece);
            Assert.Throws<ArgumentOutOfRangeException>(() => { PlacedPiece _ = layout[end - 4]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { PlacedPiece _ = layout[end]; });

            layout.EnsureCovered(layout.EndDistance + 500.0);
            Assert.Greater(layout.EndIndex, end);
            Assert.AreEqual(layout[end - 1].ExitDistance, layout[end].EntryDistance, JointTolerance);
        }

        [Test]
        public void DiscardBefore_AlwaysKeepsTheNewestPiece()
        {
            RoadLayout layout = RoadTestPieces.Layout(12);
            layout.EnsureCovered(500.0);
            int last = layout.EndIndex - 1;

            layout.DiscardBefore(layout.EndIndex + 100);

            Assert.AreEqual(last, layout.FirstIndex);
            Assert.AreEqual(last + 1, layout.EndIndex);
            Assert.DoesNotThrow(() => layout.EnsureCovered(layout.EndDistance + 100.0));
        }

        [Test]
        public void DiscardingDoesNotChangeTheRoad()
        {
            RoadLayout whole = RoadTestPieces.Layout(13);
            RoadLayout trimmed = RoadTestPieces.Layout(13);

            for (int step = 1; step <= 200; step++)
            {
                trimmed.EnsureCovered(step * 40.0);
                trimmed.DiscardBefore(trimmed.EndIndex - 4);
            }

            whole.EnsureCovered(200 * 40.0);
            for (int i = trimmed.FirstIndex; i < trimmed.EndIndex; i++)
            {
                Assert.AreEqual(whole[i].Piece.Id, trimmed[i].Piece.Id, $"Piece {i}.");
                Assert.AreEqual(whole[i].Entry.X, trimmed[i].Entry.X, JointTolerance);
            }
        }
    }
}
