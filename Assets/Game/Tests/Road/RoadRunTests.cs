using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace WrongLine.Road.Tests
{
    public class RoadRunTests
    {
        private const double Ahead = 300.0;
        private const double Behind = 60.0;
        private const double Threshold = 500.0;

        [Test]
        public void TheFirstAdvance_BringsInTheWindowAroundTheStart()
        {
            RoadRun run = Run(1);

            run.Advance(0.0);

            Assert.AreEqual(0, run.Despawned.Count);
            Assert.AreEqual(run.Spawned.Count, run.ActiveCount);
            Assert.AreEqual(0, run.Spawned[0]);
            Assert.IsFalse(run.OriginShifted);
            Assert.AreEqual(0.0, run.Cursor.Distance);
            Assert.GreaterOrEqual(run.Path[run.LastActive].ExitDistance, Ahead);
        }

        [Test]
        public void SameSeed_SameRoad_DifferentSeed_DifferentRoad()
        {
            List<int> a = Ids(Run(5), 5000);
            List<int> b = Ids(Run(5), 5000);
            List<int> c = Ids(Run(6), 5000);

            CollectionAssert.AreEqual(a, b);
            CollectionAssert.AreNotEqual(a, c);
            Assert.AreEqual(5UL, Run(5).Seed);
        }

        [Test]
        public void OneHundredKilometres_KeepsEveryInvariantOnEveryTick()
        {
            RoadRun run = Run(8);
            var active = new HashSet<int>();
            int shiftsSeen = 0;
            int mostActive = 0;

            const int Ticks = 33_334;
            for (int tick = 0; tick <= Ticks; tick++)
            {
                run.Advance(tick == 0 ? 0.0 : 3.0);

                foreach (int index in run.Despawned)
                {
                    Assert.IsTrue(active.Remove(index), $"Tick {tick}: despawned {index} which was not active.");
                }

                foreach (int index in run.Spawned)
                {
                    Assert.IsTrue(active.Add(index), $"Tick {tick}: spawned {index} twice.");
                    Assert.DoesNotThrow(() => { PlacedPiece _ = run.Path[index]; }, $"Tick {tick}: piece {index} was dropped from the path before it was shown.");
                }

                Assert.AreEqual(run.ActiveCount, active.Count);
                Assert.IsTrue(active.Contains(run.Cursor.PieceIndex), $"Tick {tick}: the cursor's piece is not in the window.");
                Assert.AreEqual(run.FirstActive, run.Path.FirstIndex, "Nothing before the window is kept.");
                mostActive = Math.Max(mostActive, active.Count);

                if (run.OriginShifted)
                {
                    shiftsSeen++;
                }

                Assert.AreEqual(shiftsSeen, run.ShiftCount);
                Assert.LessOrEqual(run.Cursor.WorldPosition.RelativeTo(run.Origin).magnitude, Threshold + 1e-3f, $"Tick {tick}: too far from the scene origin.");

                if (run.OriginShifted || tick % 500 == 0)
                {
                    AssertSeamsClosed(run, tick);
                }
            }

            Assert.Greater(shiftsSeen, 40);
            Assert.LessOrEqual(mostActive, (Ahead + Behind) / 20.0 + 2.0);
            Assert.AreEqual(Ticks * 3.0, run.Cursor.Distance, 1e-3, "About 100 km: tick 0 only sets the run up, every other tick drives 3 m.");
        }

        [Test]
        public void OnAShift_TheVectorIsHowFarTheOriginMoved()
        {
            RoadRun run = Run(2);
            run.Advance(0.0);
            WorldPoint before = run.Origin;

            while (!run.OriginShifted)
            {
                run.Advance(10.0);
            }

            WorldPoint after = run.Origin;
            Assert.AreEqual(0f, Vector3.Distance(after.RelativeTo(before), run.OriginShiftVector), 1e-3f);
            Assert.AreEqual(1, run.ShiftCount);
            Assert.AreEqual(0f, run.Cursor.WorldPosition.RelativeTo(after).magnitude, 1e-3f, "The follower is at the scene origin after a shift.");
        }

        [Test]
        public void WithoutAStartPiece_ItIsRejected()
        {
            List<RoadPieceInfo> noStart = RoadTestPieces.All().Where(piece => !piece.IsStart).ToList();

            Assert.Throws<ArgumentException>(() => new RoadRun(noStart, new RoadGeneratorSettings(), 1, Ahead, Behind, Threshold));
        }

        [TestCase(5, 99UL, 5UL)]
        [TestCase(0, 99UL, 99UL)]
        [TestCase(-1, 99UL, 4294967295UL)]
        public void TheSeed_IsTheConfiguredOne_OrARandomOneForZero(int configured, ulong random, ulong expected)
        {
            Assert.AreEqual(expected, RoadSeed.Resolve(configured, random));
        }

        private static RoadRun Run(ulong seed)
        {
            return new RoadRun(RoadTestPieces.All(), new RoadGeneratorSettings(), seed, Ahead, Behind, Threshold);
        }

        private static List<int> Ids(RoadRun run, int pieces)
        {
            run.Advance(0.0);
            while (run.LastActive < pieces)
            {
                run.Advance(100.0);
            }

            var ids = new List<int>();
            for (int i = run.FirstActive; i <= run.LastActive; i++)
            {
                ids.Add(run.Path[i].Piece.Id);
            }

            return ids;
        }

        // Every neighbouring pair of pieces in the window meets within a millimetre in the scene, as the streamer places them.
        private static void AssertSeamsClosed(RoadRun run, int tick)
        {
            for (int i = run.FirstActive; i < run.LastActive; i++)
            {
                PlacedPiece a = run.Path[i];
                PlacedPiece b = run.Path[i + 1];
                Vector3 aEnd = RoadPlacement.Position(a, run.Origin) + RoadPlacement.Rotation(a) * a.Piece.Shape.Exit.Position;
                Vector3 bStart = RoadPlacement.Position(b, run.Origin);

                Assert.Less(Vector3.Distance(aEnd, bStart), 1e-3f, $"Tick {tick}: seam between pieces {i} and {i + 1}.");
            }
        }
    }
}
