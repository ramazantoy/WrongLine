using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace WrongLine.Road.Tests
{
    public class RoadWindowTests
    {
        private const double Ahead = 300.0;
        private const double Behind = 60.0;

        [Test]
        public void TheFirstUpdate_BringsInEveryPieceFromBehindToAhead()
        {
            RoadLayout layout = RoadTestPieces.Layout(1);
            var cursor = new RoadCursor(layout);
            var window = new RoadWindow(layout, Ahead, Behind);
            var spawned = new List<int>();
            var despawned = new List<int>();

            window.Update(cursor, spawned, despawned);

            CollectionAssert.IsEmpty(despawned);
            CollectionAssert.AreEqual(Expected(layout, 0.0), spawned);
            Assert.GreaterOrEqual(layout[window.Last].ExitDistance, Ahead);
            Assert.Less(layout[window.Last].EntryDistance, Ahead, "No piece beyond the one that reaches the limit.");
            Assert.AreEqual(0, window.First);
        }

        [Test]
        public void DrivingOn_ReportsExactlyWhatEntersAndLeaves_ComparedToABruteForceScan()
        {
            RoadLayout layout = RoadTestPieces.Layout(2);
            var cursor = new RoadCursor(layout);
            var window = new RoadWindow(layout, Ahead, Behind);
            var spawned = new List<int>();
            var despawned = new List<int>();
            var active = new HashSet<int>();
            int largest = 0;

            for (int tick = 0; tick < 1700; tick++)
            {
                window.Update(cursor, spawned, despawned);

                foreach (int index in despawned)
                {
                    Assert.IsTrue(active.Remove(index), $"Despawned piece {index} that was not active.");
                }

                foreach (int index in spawned)
                {
                    Assert.IsTrue(active.Add(index), $"Spawned piece {index} twice.");
                }

                CollectionAssert.AreEquivalent(Expected(layout, cursor.Distance), active, $"At {cursor.Distance:F0} m.");
                Assert.IsTrue(window.Contains(cursor.PieceIndex), "The piece under the cursor is always in the window.");
                CollectionAssert.IsOrdered(spawned);
                CollectionAssert.IsOrdered(despawned);
                largest = Math.Max(largest, window.Count);

                cursor.Advance(3.0);
            }

            Assert.LessOrEqual(largest, (Ahead + Behind) / 20.0 + 2, "The shortest piece is 20 m, so the window cannot hold more than this.");
        }

        [Test]
        public void StandingStill_ChangesNothing()
        {
            RoadLayout layout = RoadTestPieces.Layout(3);
            var cursor = new RoadCursor(layout);
            var window = new RoadWindow(layout, Ahead, Behind);
            var spawned = new List<int>();
            var despawned = new List<int>();
            cursor.Advance(500.0);
            window.Update(cursor, spawned, despawned);

            window.Update(cursor, spawned, despawned);

            CollectionAssert.IsEmpty(spawned);
            CollectionAssert.IsEmpty(despawned);
        }

        [Test]
        public void TrimmingTheRoadBehindTheWindow_DoesNotDisturbIt()
        {
            RoadLayout layout = RoadTestPieces.Layout(4);
            var cursor = new RoadCursor(layout);
            var window = new RoadWindow(layout, Ahead, Behind);
            var spawned = new List<int>();
            var despawned = new List<int>();

            for (int tick = 0; tick < 3000; tick++)
            {
                window.Update(cursor, spawned, despawned);
                layout.DiscardBefore(window.First);
                cursor.Advance(3.0);
            }

            Assert.AreEqual(window.First, layout.FirstIndex);
            Assert.Less(layout.EndIndex - layout.FirstIndex, 25, "Only the window's pieces are kept, not nine kilometres of road.");
        }

        [Test]
        public void WithNothingBehind_TheCursorPieceIsTheFirst()
        {
            RoadLayout layout = RoadTestPieces.Layout(5);
            var cursor = new RoadCursor(layout);
            var window = new RoadWindow(layout, Ahead, 0.0);
            cursor.Advance(400.0);

            window.Update(cursor, new List<int>(), new List<int>());

            Assert.AreEqual(cursor.PieceIndex, window.First);
        }

        [TestCase(0.0, 10.0)]
        [TestCase(-5.0, 10.0)]
        [TestCase(100.0, -1.0)]
        public void NonsenseDistances_AreRejected(double ahead, double behind)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoadWindow(RoadTestPieces.Layout(1), ahead, behind));
        }

        // Every piece that reaches back to (distance - Behind) and starts before (distance + Ahead), found by scanning them all.
        private static List<int> Expected(RoadLayout layout, double distance)
        {
            layout.EnsureCovered(distance + Ahead);
            var expected = new List<int>();
            for (int i = layout.FirstIndex; i < layout.EndIndex; i++)
            {
                if (layout[i].ExitDistance >= distance - Behind && layout[i].EntryDistance < distance + Ahead)
                {
                    expected.Add(i);
                }
            }

            return expected;
        }
    }
}
