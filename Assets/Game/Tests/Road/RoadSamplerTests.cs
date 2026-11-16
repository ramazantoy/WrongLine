using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace WrongLine.Road.Tests
{
    public class RoadSamplerTests
    {
        private static RoadLayout Covered(ulong seed, double distance)
        {
            RoadLayout layout = RoadTestPieces.Layout(seed);
            layout.EnsureCovered(distance);
            return layout;
        }

        [Test]
        public void AtTheCursorsDistance_GivesTheCursorsPose()
        {
            RoadLayout layout = Covered(4, 3000.0);
            var cursor = new RoadCursor(layout);

            for (int i = 0; i < 200; i++)
            {
                cursor.Advance(13.7);
                RoadSample sample = RoadSampler.At(layout, cursor.Distance);

                Assert.AreEqual(0.0, cursor.WorldPosition.DistanceTo(sample.Position), 1e-4);
                Assert.Less(Quaternion.Angle(cursor.WorldRotation, sample.Rotation), 0.01f);
            }
        }

        [Test]
        public void AcrossEveryJoint_TheRoadIsContinuous()
        {
            RoadLayout layout = Covered(9, 2500.0);

            for (int index = layout.FirstIndex + 1; index < layout.EndIndex; index++)
            {
                double joint = layout[index].EntryDistance;
                foreach (float lateral in new[] { 0f, 3f, -3f })
                {
                    RoadSample before = RoadSampler.At(layout, joint - 0.001, lateral, 0.5f);
                    RoadSample after = RoadSampler.At(layout, joint + 0.001, lateral, 0.5f);

                    Assert.Less(before.Position.DistanceTo(after.Position), 0.01, $"joint {index}, lateral {lateral}");
                    Assert.Less(Quaternion.Angle(before.Rotation, after.Rotation), 0.1f);
                }
            }
        }

        [Test]
        public void ASidewaysOffset_FollowsTheRoadsOwnRight_NotTheWorldsRight()
        {
            List<RoadPieceInfo> all = RoadTestPieces.All();
            RoadPieceInfo banked = all.First(piece => (piece.Tags & RoadTags.Banked) != 0 && piece.Name.EndsWith("_R"));
            RoadLayout layout = RoadTestPieces.Layout(1, null, new List<RoadPieceInfo> { RoadTestPieces.Start(all), banked });
            layout.EnsureCovered(layout[0].Piece.Length + 1.0);
            PlacedPiece piece = layout[1];
            double middle = piece.EntryDistance + piece.Piece.Length * 0.5;

            RoadSample centre = RoadSampler.At(layout, middle);
            RoadSample right = RoadSampler.At(layout, middle, 3f);

            Vector3 offset = new Vector3((float)(right.Position.X - centre.Position.X), (float)(right.Position.Y - centre.Position.Y), (float)(right.Position.Z - centre.Position.Z));
            Assert.AreEqual(0f, Vector3.Distance(offset, centre.Rotation * new Vector3(3f, 0f, 0f)), 1e-4f);
            Assert.Greater(Mathf.Abs(offset.y), 0.05f, "On a banked piece the right-hand side is higher or lower than the centre.");
        }

        [Test]
        public void AHeightOffset_IsAlongTheRoadsUp()
        {
            RoadLayout layout = Covered(2, 800.0);
            double distance = 321.0;

            RoadSample on = RoadSampler.At(layout, distance);
            RoadSample above = RoadSampler.At(layout, distance, 0f, 2f);

            Vector3 offset = new Vector3((float)(above.Position.X - on.Position.X), (float)(above.Position.Y - on.Position.Y), (float)(above.Position.Z - on.Position.Z));
            Assert.AreEqual(0f, Vector3.Distance(offset, on.Rotation * new Vector3(0f, 2f, 0f)), 1e-4f);
        }

        [Test]
        public void BehindTheOldestPiece_TheRoadRunsStraightOn()
        {
            RoadLayout layout = Covered(1, 100.0);

            RoadSample behind = RoadSampler.At(layout, -10.0, 2f);

            Assert.AreEqual(0.0, behind.Position.DistanceTo(new WorldPoint(2, 0, -10)), 1e-6);
            Assert.Less(Quaternion.Angle(Quaternion.identity, behind.Rotation), 0.001f);
        }

        [Test]
        public void PastTheNewestPiece_ClampsToItsEnd()
        {
            RoadLayout layout = Covered(1, 300.0);

            RoadSample end = RoadSampler.At(layout, layout.EndDistance);
            RoadSample past = RoadSampler.At(layout, layout.EndDistance + 50.0);

            Assert.AreEqual(0.0, end.Position.DistanceTo(past.Position), 1e-6);
        }

        [Test]
        public void ADroppedPieceBehind_StillSamplesTheOldestKept()
        {
            RoadLayout layout = Covered(3, 1500.0);
            layout.DiscardBefore(5);

            double distance = layout[layout.FirstIndex].EntryDistance + 5.0;
            RoadSample sample = RoadSampler.At(layout, distance);

            Assert.AreEqual(0.0, sample.Position.DistanceTo(layout[layout.FirstIndex].Entry.TransformPoint(layout[layout.FirstIndex].Piece.Shape.Evaluate(5f).Position)), 1e-6);
        }
    }
}
