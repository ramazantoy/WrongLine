using System;
using System.Collections.Generic;
using UnityEngine;
using WrongLine.Road;

namespace WrongLine.Scenery
{
    /// <summary>
    /// Decides the props beside one road piece. Pure and repeatable: the same run seed, piece and road give the same props.
    /// A prop stands on the ground (<see cref="GroundProfile"/>), upright, and never on or too near the road: every candidate is
    /// measured against the centre line of the road around it, which catches the inside of a tight bend and the next piece.
    /// In a biome change the props of both biomes mix, each kept with its biome's weight there.
    /// </summary>
    public sealed class SceneryPlanner
    {
        /// <summary>Room a prop keeps from the edge of the curb beyond its own footprint, in metres (covers the sampling step too).</summary>
        public const float ClearanceMargin = 1.5f;

        private const float SampleStep = 2f;
        private const float ScanSlack = 5f;

        private readonly IReadOnlyList<PropRule> _rules;
        private readonly BiomeSchedule _schedule;
        private readonly GroundSettings _ground;
        private readonly ulong _seed;
        private readonly List<(Vector3 Position, float Footprint)> _taken = new List<(Vector3, float)>();

        public SceneryPlanner(IReadOnlyList<PropRule> rules, BiomeSchedule schedule, GroundSettings ground, ulong seed)
        {
            ground.Validate();
            _rules = rules;
            _schedule = schedule;
            _ground = ground;
            _seed = seed;
        }

        /// <summary>The closest a prop with this footprint may stand to the centre line of any part of the road.</summary>
        public static float Clearance(float footprint)
        {
            return GroundProfile.EdgeX + footprint + ClearanceMargin;
        }

        /// <summary>Adds the props of piece <paramref name="index"/> to <paramref name="into"/>.</summary>
        public void Plan(IRoadPath path, int index, List<SceneryItem> into)
        {
            PlacedPiece placed = path[index];
            var random = new RoadRandom(PieceSeed(_seed, index));
            _taken.Clear();

            foreach (PropRule rule in _rules)
            {
                if (!PresentOn(rule, placed))
                {
                    continue;
                }

                if (rule.Mode == PropMode.Row)
                {
                    PlanRow(path, placed, rule, random, into);
                }
                else
                {
                    PlanScatter(path, placed, rule, random, into);
                }
            }
        }

        /// <summary>A seed of its own for every piece, so a piece's props do not depend on the pieces before it.</summary>
        public static ulong PieceSeed(ulong seed, int index)
        {
            unchecked
            {
                return new RoadRandom(seed ^ ((ulong)index * 0xD1B54A32D192ED03UL)).NextUInt64();
            }
        }

        private bool PresentOn(PropRule rule, in PlacedPiece placed)
        {
            if (rule.Biome == PropRule.Shared)
            {
                return true;
            }

            // A blend band is shorter than any biome, so looking at both ends and the middle cannot miss one.
            double middle = (placed.EntryDistance + placed.ExitDistance) * 0.5;
            return _schedule.Weight(rule.Biome, placed.EntryDistance) > 0f
                || _schedule.Weight(rule.Biome, middle) > 0f
                || _schedule.Weight(rule.Biome, placed.ExitDistance) > 0f;
        }

        private void PlanRow(IRoadPath path, in PlacedPiece placed, PropRule rule, RoadRandom random, List<SceneryItem> into)
        {
            // Rows count in whole road distance, so they run on evenly across joints.
            long first = (long)Math.Ceiling(placed.EntryDistance / rule.Spacing);
            for (long k = first; k * rule.Spacing < placed.ExitDistance; k++)
            {
                double distance = k * rule.Spacing;
                float side = rule.Side == RowSide.Right ? 1f : rule.Side == RowSide.Left ? -1f : (k % 2 == 0 ? 1f : -1f);
                bool kept = random.NextDouble() < Weight(rule, distance);
                if (kept)
                {
                    // Faces the road: its +Z points back at the centre line.
                    TryPlace(path, placed, rule, (float)(distance - placed.EntryDistance), side * rule.LateralMin, side > 0f ? -90f : 90f, 1f, into);
                }
            }
        }

        private void PlanScatter(IRoadPath path, in PlacedPiece placed, PropRule rule, RoadRandom random, List<SceneryItem> into)
        {
            double expected = rule.Density * placed.Piece.Length / 100.0;
            int count = (int)expected + (random.NextDouble() < expected - Math.Floor(expected) ? 1 : 0);
            for (int i = 0; i < count; i++)
            {
                // Every draw happens whether the candidate survives or not, so one change cannot shift the rest.
                float s = (float)(random.NextDouble() * placed.Piece.Length);
                float side = random.NextDouble() < 0.5 ? -1f : 1f;
                float lateral = Mathf.Lerp(rule.LateralMin, rule.LateralMax, (float)random.NextDouble());
                float yaw = (float)(random.NextDouble() * 360.0);
                float scale = Mathf.Lerp(rule.ScaleMin, rule.ScaleMax, (float)random.NextDouble());
                bool kept = random.NextDouble() < Weight(rule, placed.EntryDistance + s);
                if (kept)
                {
                    TryPlace(path, placed, rule, s, side * lateral, yaw, scale, into, relativeYaw: false);
                }
            }
        }

        private float Weight(PropRule rule, double distance)
        {
            return rule.Biome == PropRule.Shared ? 1f : _schedule.Weight(rule.Biome, distance);
        }

        private void TryPlace(IRoadPath path, in PlacedPiece placed, PropRule rule, float s, float x, float yaw, float scale, List<SceneryItem> into, bool relativeYaw = true)
        {
            TrackShape shape = placed.Piece.Shape;
            int side = x >= 0f ? 1 : -1;
            float reach = GroundProfile.Reach(shape, s, side, _ground);
            if (Math.Abs(x) + rule.Footprint > reach)
            {
                return; // No ground there (the inside of a bend).
            }

            Vector3 local = GroundProfile.Ground(shape, s, x, reach, _ground.EdgeDrop);
            foreach ((Vector3 position, float footprint) in _taken)
            {
                float dx = position.x - local.x;
                float dz = position.z - local.z;
                float room = footprint + rule.Footprint;
                if (dx * dx + dz * dz < room * room)
                {
                    return; // On top of another prop.
                }
            }

            WorldPoint world = placed.Entry.TransformPoint(local);
            if (!ClearOfRoad(path, placed.EntryDistance + s, world, Clearance(rule.Footprint), Math.Abs(x)))
            {
                return;
            }

            _taken.Add((local, rule.Footprint));
            float heading = (float)(placed.Entry.YawDegrees + GroundProfile.Heading(shape, s) * (180.0 / Math.PI));
            into.Add(new SceneryItem(rule.Id, world, relativeYaw ? heading + yaw : yaw, scale));
        }

        /// <summary>Whether <paramref name="point"/> is at least <paramref name="clearance"/> (on the level) from the road's centre line near it.</summary>
        private static bool ClearOfRoad(IRoadPath path, double distance, in WorldPoint point, float clearance, float lateral)
        {
            double reach = lateral + clearance + ScanSlack;
            double from = Math.Max(path[path.FirstIndex].EntryDistance, distance - reach);
            double to = Math.Min(path.EndDistance, distance + reach);
            double limit = (double)clearance * clearance;
            for (double t = from; t <= to + 1e-6; t += SampleStep)
            {
                WorldPoint centre = RoadSampler.At(path, Math.Min(t, to)).Position;
                double dx = centre.X - point.X;
                double dz = centre.Z - point.Z;
                if (dx * dx + dz * dz < limit)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
