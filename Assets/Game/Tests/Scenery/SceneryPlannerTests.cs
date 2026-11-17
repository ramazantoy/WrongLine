using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WrongLine.Road;
using WrongLine.Road.Tests;

namespace WrongLine.Scenery.Tests
{
    public class SceneryPlannerTests
    {
        private static readonly GroundSettings _ground = new GroundSettings(0.3f, 80f, 0.8f, 20f, 2f);
        private static readonly BiomeSettings _biomes = new BiomeSettings(1500f, 3000f, 150f);

        private static PropRule Scatter(int id, int biome, float density, float lateralMin, float lateralMax, float footprint)
        {
            return new PropRule(id, biome, PropMode.Scatter, density, 0f, RowSide.Alternate, lateralMin, lateralMax, footprint, 0.8f, 1.2f);
        }

        private static PropRule Row(int id, float spacing, RowSide side, float lateral)
        {
            return new PropRule(id, PropRule.Shared, PropMode.Row, 0f, spacing, side, lateral, lateral, 0.4f, 1f, 1f);
        }

        /// <summary>A road of straights only: it runs along +Z from the origin, so world X is the lateral and Z the distance.</summary>
        private static RoadLayout StraightRoad(double length)
        {
            List<RoadPieceInfo> all = RoadTestPieces.All();
            RoadLayout layout = RoadTestPieces.Layout(1, null, new List<RoadPieceInfo> { RoadTestPieces.Start(all), RoadTestPieces.Named(all, "Straight_M") });
            layout.EnsureCovered(length);
            return layout;
        }

        private static List<SceneryItem> PlanAll(SceneryPlanner planner, IRoadPath path, int from, int to)
        {
            var items = new List<SceneryItem>();
            for (int index = from; index < to; index++)
            {
                planner.Plan(path, index, items);
            }

            return items;
        }

        [Test]
        public void TheSameSeedAndPiece_GiveTheSameProps_AnotherSeedOthers()
        {
            RoadLayout layout = RoadTestPieces.Layout(3);
            layout.EnsureCovered(2000.0);
            var rules = new[] { Scatter(0, PropRule.Shared, 8f, 10f, 40f, 1f), Row(1, 40f, RowSide.Alternate, 9f) };
            var schedule = new BiomeSchedule(1, 1, _biomes);

            List<SceneryItem> a = PlanAll(new SceneryPlanner(rules, schedule, _ground, 77), layout, 1, layout.EndIndex - 1);
            List<SceneryItem> b = PlanAll(new SceneryPlanner(rules, schedule, _ground, 77), layout, 1, layout.EndIndex - 1);
            List<SceneryItem> c = PlanAll(new SceneryPlanner(rules, schedule, _ground, 78), layout, 1, layout.EndIndex - 1);

            Assert.Greater(a.Count, 50);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].PropId, b[i].PropId);
                Assert.AreEqual(0.0, a[i].Position.DistanceTo(b[i].Position), 1e-9);
                Assert.AreEqual(a[i].YawDegrees, b[i].YawDegrees);
                Assert.AreEqual(a[i].Scale, b[i].Scale);
            }

            Assert.IsFalse(a.Count == c.Count && a.Zip(c, (x, y) => x.Position.DistanceTo(y.Position) < 1e-6).All(same => same), "Another seed, other props.");
        }

        [Test]
        public void APiecesProps_DoNotDependOnWhichPiecesWerePlannedBefore()
        {
            RoadLayout layout = RoadTestPieces.Layout(8);
            layout.EnsureCovered(1500.0);
            var rules = new[] { Scatter(0, PropRule.Shared, 10f, 10f, 40f, 1f) };
            var schedule = new BiomeSchedule(1, 1, _biomes);
            int index = layout.EndIndex - 3;

            var alone = new List<SceneryItem>();
            new SceneryPlanner(rules, schedule, _ground, 5).Plan(layout, index, alone);
            var planner = new SceneryPlanner(rules, schedule, _ground, 5);
            PlanAll(planner, layout, 1, index);
            var afterOthers = new List<SceneryItem>();
            planner.Plan(layout, index, afterOthers);

            Assert.AreEqual(alone.Count, afterOthers.Count);
            Assert.IsTrue(alone.Zip(afterOthers, (x, y) => x.Position.DistanceTo(y.Position) < 1e-9).All(same => same));
        }

        [Test]
        public void NoProp_StandsOnOrNearTheRoad_OnAnyRoad_EvenInsideTightBends()
        {
            // Lateral minimums right at the clearance, so only the road check keeps props off the road in the bends.
            const float Footprint = 1.5f;
            float clearance = SceneryPlanner.Clearance(Footprint);
            var rules = new[] { Scatter(0, PropRule.Shared, 40f, clearance, 30f, Footprint) };
            var schedule = new BiomeSchedule(1, 1, _biomes);
            int checkedProps = 0;

            for (ulong seed = 1; seed <= 6; seed++)
            {
                RoadLayout layout = RoadTestPieces.Layout(seed);
                layout.EnsureCovered(3000.0);
                var planner = new SceneryPlanner(rules, schedule, _ground, seed);
                List<SceneryItem> items = PlanAll(planner, layout, 1, layout.EndIndex - 1);

                // The road as finely as the test can afford: every 0.25 m.
                var centre = new List<WorldPoint>();
                for (double d = 0.0; d <= layout.EndDistance; d += 0.25)
                {
                    centre.Add(RoadSampler.At(layout, d).Position);
                }

                foreach (SceneryItem item in items)
                {
                    double nearest = centre.Min(point => Math.Sqrt((point.X - item.Position.X) * (point.X - item.Position.X) + (point.Z - item.Position.Z) * (point.Z - item.Position.Z)));
                    Assert.GreaterOrEqual(nearest, clearance - 0.1, $"seed {seed}: a prop at {item.Position} is {nearest:F2} m from the road.");
                    checkedProps++;
                }
            }

            Assert.Greater(checkedProps, 1000);
        }

        [Test]
        public void APropAskedToStandTooNearTheRoad_IsNeverPlaced()
        {
            // The ground is there (a straight), only the road check can refuse it.
            RoadLayout layout = StraightRoad(800.0);
            float lateral = SceneryPlanner.Clearance(0.4f) - 1f;
            var planner = new SceneryPlanner(new[] { Row(0, 20f, RowSide.Alternate, lateral), Row(1, 20f, RowSide.Alternate, lateral + 2f) }, new BiomeSchedule(1, 1, _biomes), _ground, 3);

            List<SceneryItem> items = PlanAll(planner, layout, 0, layout.EndIndex - 1);

            Assert.IsTrue(items.All(item => item.PropId == 1), "Only the row that keeps its distance stands.");
            Assert.Greater(items.Count, 20);
        }

        [Test]
        public void Props_StandOnTheGround_WithinTheirBand_AtTheirScale_AndNeverOnTopOfEachOther()
        {
            RoadLayout layout = StraightRoad(4000.0);
            var rules = new[] { Scatter(0, PropRule.Shared, 12f, 11f, 40f, 2f), Scatter(1, PropRule.Shared, 12f, 11f, 40f, 2f) };
            var planner = new SceneryPlanner(rules, new BiomeSchedule(1, 1, _biomes), _ground, 21);

            int lastPiece = layout.EndIndex - 1;
            for (int index = 1; index < lastPiece; index++)
            {
                var items = new List<SceneryItem>();
                planner.Plan(layout, index, items);
                foreach (SceneryItem item in items)
                {
                    Assert.That(Math.Abs(item.Position.X), Is.InRange(11.0 - 1e-4, 40.0 + 1e-4));
                    Assert.AreEqual(-_ground.EdgeDrop, item.Position.Y, 1e-4, "On the ground, which lies below the asphalt.");
                    Assert.That(item.Scale, Is.InRange(0.8f, 1.2f));
                    Assert.That(item.Position.Z, Is.InRange(layout[index].EntryDistance - 1e-4, layout[index].ExitDistance + 1e-4), "Within its piece.");
                }

                for (int i = 0; i < items.Count; i++)
                {
                    for (int j = i + 1; j < items.Count; j++)
                    {
                        Assert.GreaterOrEqual(items[i].Position.DistanceTo(items[j].Position), 4.0 - 1e-4, "Two footprints of 2 m never overlap.");
                    }
                }
            }
        }

        [Test]
        public void TheScatterDensity_IsPropsPer100Metres()
        {
            RoadLayout layout = StraightRoad(20000.0);
            var rules = new[] { Scatter(0, PropRule.Shared, 6f, 11f, 60f, 0.5f) };
            var planner = new SceneryPlanner(rules, new BiomeSchedule(1, 1, _biomes), _ground, 4);

            List<SceneryItem> items = PlanAll(planner, layout, 1, layout.EndIndex - 1);
            double length = layout[layout.EndIndex - 2].ExitDistance - layout[1].EntryDistance;

            Assert.AreEqual(6.0, items.Count * 100.0 / length, 0.6);
            int right = items.Count(item => item.Position.X > 0.0);
            Assert.AreEqual(0.5, (double)right / items.Count, 0.06, "Both sides alike.");
        }

        [Test]
        public void ARow_StandsEverySpacingMetresOfRoad_AlternatingSides_FacingTheRoad()
        {
            RoadLayout layout = StraightRoad(1000.0);
            var planner = new SceneryPlanner(new[] { Row(0, 40f, RowSide.Alternate, 9f) }, new BiomeSchedule(1, 1, _biomes), _ground, 2);

            List<SceneryItem> items = PlanAll(planner, layout, 0, layout.EndIndex - 1).OrderBy(item => item.Position.Z).ToList();

            Assert.Greater(items.Count, 15);
            for (int i = 0; i < items.Count; i++)
            {
                SceneryItem item = items[i];
                double k = item.Position.Z / 40.0;
                Assert.AreEqual(Math.Round(k), k, 1e-4, "On a multiple of the spacing.");
                bool rightSide = Math.Round(k) % 2 == 0;
                Assert.AreEqual(rightSide ? 9.0 : -9.0, item.Position.X, 1e-4);
                Assert.AreEqual(rightSide ? -90f : 90f, item.YawDegrees, 1e-3f, "Its +Z points at the road.");
                if (i > 0)
                {
                    Assert.AreEqual(40.0, item.Position.Z - items[i - 1].Position.Z, 1e-4, "None missing, none doubled at a joint.");
                }
            }
        }

        [Test]
        public void ARowOnOneSide_StaysOnThatSide()
        {
            RoadLayout layout = StraightRoad(600.0);
            var planner = new SceneryPlanner(new[] { Row(0, 30f, RowSide.Left, 12f) }, new BiomeSchedule(1, 1, _biomes), _ground, 2);

            List<SceneryItem> items = PlanAll(planner, layout, 0, layout.EndIndex - 1);

            Assert.Greater(items.Count, 10);
            Assert.IsTrue(items.All(item => Math.Abs(item.Position.X + 12.0) < 1e-4));
        }

        [Test]
        public void ABiomesProps_StandOnlyInItsStretch_AndMixInTheBlendBand()
        {
            var biomes = new BiomeSettings(1500f, 1500f, 300f);
            var schedule = new BiomeSchedule(12, 2, biomes);
            Assume.That(schedule.TryTransition(0.0, 1e6, out BiomeTransition change));
            RoadLayout layout = StraightRoad(change.Distance + 1200.0);
            var rules = new[] { Scatter(0, 0, 30f, 11f, 40f, 0.5f), Scatter(1, 1, 30f, 11f, 40f, 0.5f) };
            var planner = new SceneryPlanner(rules, schedule, _ground, 8);

            List<SceneryItem> items = PlanAll(planner, layout, 1, layout.EndIndex - 1);
            double half = biomes.BlendMeters * 0.5;
            int mixedFrom = 0;
            int mixedTo = 0;
            foreach (SceneryItem item in items)
            {
                int biome = rules[item.PropId].Biome;
                if (item.Position.Z < change.Distance - half)
                {
                    Assert.AreEqual(change.From, biome, $"at {item.Position.Z}");
                }
                else if (item.Position.Z > change.Distance + half)
                {
                    Assert.AreEqual(change.To, biome, $"at {item.Position.Z}");
                }
                else if (biome == change.From)
                {
                    mixedFrom++;
                }
                else
                {
                    mixedTo++;
                }
            }

            Assert.Greater(mixedFrom, 5, "The old biome's props reach into the band.");
            Assert.Greater(mixedTo, 5, "The new biome's props start in the band.");
        }

        [Test]
        public void Clearance_IsTheCurbsEdgePlusTheFootprintPlusTheMargin()
        {
            Assert.AreEqual(GroundProfile.EdgeX + 2f + SceneryPlanner.ClearanceMargin, SceneryPlanner.Clearance(2f), 1e-5f);
        }
    }
}
