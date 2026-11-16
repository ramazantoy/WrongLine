using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace WrongLine.Road.Tests
{
    public class RoadGeneratorTests
    {
        private const int Pieces = 3000;

        [Test]
        public void SameSeed_GivesTheSameRoad_DifferentSeedsDiffer()
        {
            int[] a = Ids(RoadTestPieces.Layout(42), 1000);
            int[] b = Ids(RoadTestPieces.Layout(42), 1000);
            int[] c = Ids(RoadTestPieces.Layout(43), 1000);

            CollectionAssert.AreEqual(a, b);
            CollectionAssert.AreNotEqual(a.Take(60).ToArray(), c.Take(60).ToArray());
        }

        [Test]
        public void Heading_NeverLeavesTheYawBand_AnywhereAlongAnyPiece()
        {
            var settings = new RoadGeneratorSettings();
            RoadLayout layout = RoadTestPieces.Layout(7, settings);
            layout.EnsureCovered(Pieces * 30.0);

            for (int i = layout.FirstIndex; i < layout.EndIndex; i++)
            {
                PlacedPiece placed = layout[i];
                if (i == 0)
                {
                    continue;
                }

                double entryYaw = placed.Entry.YawDegrees;
                Assert.LessOrEqual(entryYaw + placed.Piece.MaxYawDegrees, settings.MaxYawDegrees + 1e-3, $"Piece {i} {placed.Piece}");
                Assert.GreaterOrEqual(entryYaw + placed.Piece.MinYawDegrees, -settings.MaxYawDegrees - 1e-3, $"Piece {i} {placed.Piece}");
            }
        }

        [Test]
        public void Height_NeverLeavesTheBand_AnywhereAlongAnyPiece()
        {
            var settings = new RoadGeneratorSettings();
            RoadLayout layout = RoadTestPieces.Layout(11, settings);
            layout.EnsureCovered(Pieces * 30.0);

            for (int i = 1; i < layout.EndIndex; i++)
            {
                PlacedPiece placed = layout[i];
                Assert.GreaterOrEqual(placed.Entry.Y + placed.Piece.MinY, settings.MinHeight - 1e-3, $"Piece {i} {placed.Piece}");
                Assert.LessOrEqual(placed.Entry.Y + placed.Piece.MaxY, settings.MaxHeight + 1e-3, $"Piece {i} {placed.Piece}");
            }
        }

        [Test]
        public void TheRoadAlwaysMovesForward_SoItCannotCrossItself()
        {
            RoadLayout layout = RoadTestPieces.Layout(5);
            layout.EnsureCovered(2000 * 30.0);

            double previousZ = double.NegativeInfinity;
            for (int i = 0; i < layout.EndIndex; i++)
            {
                PlacedPiece placed = layout[i];
                for (int f = 0; f < placed.Piece.Shape.Count; f++)
                {
                    double z = placed.Entry.TransformPoint(placed.Piece.Shape[f].Position).Z;
                    if (i == 0 && f == 0)
                    {
                        previousZ = z;
                        continue;
                    }

                    if (f == 0)
                    {
                        continue; // The joint frame equals the previous piece's last frame.
                    }

                    Assert.Greater(z, previousZ, $"Piece {i} {placed.Piece}, frame {f}");
                    previousZ = z;
                }
            }
        }

        [Test]
        public void NoPieceRepeatsWithinTheWindow()
        {
            var settings = new RoadGeneratorSettings { RepeatWindow = 3 };
            int[] ids = Ids(RoadTestPieces.Layout(21, settings), Pieces);

            for (int i = 1; i < ids.Length; i++)
            {
                for (int back = 1; back <= settings.RepeatWindow && i - back >= 1; back++)
                {
                    Assert.AreNotEqual(ids[i - back], ids[i], $"Piece {i} repeats piece {i - back}.");
                }
            }
        }

        [Test]
        public void TheStartPiece_IsNeverPickedAgain_AndIsOutsideTheRules()
        {
            List<RoadPieceInfo> pieces = RoadTestPieces.All();
            RoadPieceInfo start = RoadTestPieces.Start(pieces);
            RoadLayout layout = RoadTestPieces.Layout(3, null, pieces);
            layout.EnsureCovered(Pieces * 30.0);

            Assert.AreSame(start, layout[0].Piece);
            for (int i = 1; i < layout.EndIndex; i++)
            {
                Assert.AreNotSame(start, layout[i].Piece);
            }

            // The road goes on from the start piece's exit, wherever that is.
            Assert.AreEqual(layout[0].Exit.X, layout[1].Entry.X);
            Assert.AreEqual(layout[0].Exit.Y, layout[1].Entry.Y);
            Assert.AreEqual(layout[0].Exit.Z, layout[1].Entry.Z);
            Assert.AreEqual(layout[0].Exit.Yaw, layout[1].Entry.Yaw);
            Assert.AreEqual(start.Length, layout[0].ExitDistance, 1e-9);
        }

        [Test]
        public void ABentStart_IsNotHeldToTheYawRule_TheRoadContinuesFromItsExit()
        {
            List<RoadPieceInfo> pieces = RoadTestPieces.All();
            var bentStart = new RoadPieceInfo(99, "BentStart", new TrackRecipe(40f, 60f, YawProfile.Single, 0f, PitchProfile.Bump, 0f), true);
            var others = pieces.Where(piece => !piece.IsStart).Append(bentStart).ToList();
            var layout = new RoadLayout(bentStart, new RoadGenerator(others, new RoadGeneratorSettings(), 3));

            layout.EnsureCovered(2000.0);

            Assert.AreEqual(60.0, layout[0].Exit.YawDegrees, 1e-9);
            Assert.AreEqual(layout[0].Exit.Yaw, layout[1].Entry.Yaw);
        }

        [Test]
        public void EveryKindOfPiece_Appears()
        {
            RoadLayout layout = RoadTestPieces.Layout(8);
            layout.EnsureCovered(Pieces * 30.0);

            RoadTags seen = RoadTags.None;
            for (int i = 1; i < layout.EndIndex; i++)
            {
                seen |= layout[i].Piece.Tags;
            }

            foreach (RoadTags tag in new[] { RoadTags.Straight, RoadTags.Curve, RoadTags.SCurve, RoadTags.Climb, RoadTags.Drop, RoadTags.Hill, RoadTags.Dip, RoadTags.Banked })
            {
                Assert.IsTrue(seen.HasFlag(tag), $"No {tag} piece in {layout.EndIndex} pieces.");
            }
        }

        [Test]
        public void TheRoadGetsMoreDemandingWithDistance()
        {
            double early = 0.0;
            double late = 0.0;
            int earlyCount = 0;
            int lateCount = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                RoadLayout layout = RoadTestPieces.Layout(seed);
                layout.EnsureCovered(9000.0);
                for (int i = 1; i < layout.EndIndex; i++)
                {
                    PlacedPiece placed = layout[i];
                    if (placed.EntryDistance < 1000.0)
                    {
                        early += placed.Piece.Intensity;
                        earlyCount++;
                    }
                    else if (placed.EntryDistance > 6000.0)
                    {
                        late += placed.Piece.Intensity;
                        lateCount++;
                    }
                }
            }

            Assert.Greater(late / lateCount, early / earlyCount + 0.1, $"Mean intensity: early {early / earlyCount:F3}, late {late / lateCount:F3}.");
        }

        [Test]
        public void WhenNothingFits_ItSaysSo()
        {
            List<RoadPieceInfo> pieces = RoadTestPieces.All();
            var onlyAHairpin = new List<RoadPieceInfo> { RoadTestPieces.Start(pieces), RoadTestPieces.Named(pieces, "CurveSharp_R") };
            var generator = new RoadGenerator(onlyAHairpin, new RoadGeneratorSettings(), 1);
            var layout = new RoadLayout(onlyAHairpin[0], generator);

            Assert.Throws<InvalidOperationException>(() => layout.EnsureCovered(500.0), "A 90 degree curve breaks the 75 degree band on its own.");
        }

        [Test]
        public void WhenOnlyRecentPiecesFit_TheyAreStillUsed()
        {
            List<RoadPieceInfo> pieces = RoadTestPieces.All();
            var onlyStraights = new List<RoadPieceInfo> { RoadTestPieces.Start(pieces), RoadTestPieces.Named(pieces, "Straight_M") };
            RoadLayout layout = RoadTestPieces.Layout(1, null, onlyStraights);

            Assert.DoesNotThrow(() => layout.EnsureCovered(400.0), "One selectable piece, so the repeat rule has to give way.");
            Assert.AreEqual(10, layout.EndIndex, "The 60 m start, then nine 40 m pieces reach 420 m.");
        }

        [Test]
        public void NoSelectablePieces_IsRejected()
        {
            List<RoadPieceInfo> pieces = RoadTestPieces.All();

            Assert.Throws<ArgumentException>(() => new RoadGenerator(new[] { RoadTestPieces.Start(pieces) }, new RoadGeneratorSettings(), 1));
        }

        [TestCase(nameof(RoadGeneratorSettings.MaxYawDegrees), 90f)]
        [TestCase(nameof(RoadGeneratorSettings.MaxYawDegrees), 0f)]
        [TestCase(nameof(RoadGeneratorSettings.MinHeight), 5f)]
        [TestCase(nameof(RoadGeneratorSettings.DifficultyRampMeters), 0f)]
        [TestCase(nameof(RoadGeneratorSettings.WeightStraight), 0f)]
        [TestCase(nameof(RoadGeneratorSettings.EasyIntensityPenalty), 1f)]
        public void NonsenseSettings_AreRejected(string field, float value)
        {
            var settings = new RoadGeneratorSettings();
            typeof(RoadGeneratorSettings).GetField(field).SetValue(settings, value);

            Assert.Throws<ArgumentException>(() => settings.Validate());
            Assert.Throws<ArgumentException>(() => new RoadGenerator(RoadTestPieces.All(), settings, 1));
        }

        [Test]
        public void DefaultSettings_AreValid()
        {
            Assert.DoesNotThrow(() => new RoadGeneratorSettings().Validate());
        }

        private static int[] Ids(RoadLayout layout, int count)
        {
            while (layout.EndIndex < count)
            {
                layout.EnsureCovered(layout.EndDistance + 1.0);
            }

            var ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                ids[i] = layout[i].Piece.Id;
            }

            return ids;
        }
    }
}
