using System;
using UnityEngine;
using VContainer.Unity;
using WrongLine.Driving;
using WrongLine.Road;

namespace WrongLine.Scenery
{
    /// <summary>
    /// Tells the ground shader (<c>CurvedLit</c> with <c>_GROUNDBLEND_ON</c>) the biome colours once a frame. In a biome change
    /// in sight, the ground fades from the old colour to the new across a plane at right angles to the road at the middle of the
    /// blend band; the road runs on roughly forward, so a plane is enough. Globals only: no material is written to, and the
    /// SRP Batcher keeps batching the ground. Also keeps the ground texture's world mapping still when the scene origin moves.
    /// </summary>
    internal sealed class BiomeBlendDriver : IStartable, ITickable
    {
        /// <summary>The ground texture repeats in a whole number of tiles over this many metres: its tile size must divide it.</summary>
        public const double GroundUvPeriod = 1024.0;

        private static readonly int _colorAId = Shader.PropertyToID("_WL_BiomeColorA");
        private static readonly int _colorBId = Shader.PropertyToID("_WL_BiomeColorB");
        private static readonly int _planeId = Shader.PropertyToID("_WL_BiomePlane");
        private static readonly int _directionId = Shader.PropertyToID("_WL_BiomeDir");
        private static readonly int _groundUvId = Shader.PropertyToID("_WL_GroundUV");

        private readonly IRoadFrame _road;
        private readonly BiomeSchedule _schedule;
        private readonly ISceneryPool _pool;
        private readonly RoadConfig _roadConfig;

        public BiomeBlendDriver(IRoadFrame road, BiomeSchedule schedule, ISceneryPool pool, RoadConfig roadConfig)
        {
            _road = road;
            _schedule = schedule;
            _pool = pool;
            _roadConfig = roadConfig;
        }

        public void Start()
        {
            Tick();
        }

        public void Tick()
        {
            double distance = _road.Distance;
            Color from;
            Color to;
            Vector3 point = Vector3.zero;
            Vector3 direction = Vector3.forward;
            if (_schedule.TryTransition(distance - _roadConfig.BehindMeters, distance + _roadConfig.AheadMeters, out BiomeTransition transition))
            {
                from = GroundColor(transition.From);
                to = GroundColor(transition.To);
                RoadSample sample = RoadSampler.At(_road.Path, Math.Min(transition.Distance, _road.Path.EndDistance));
                point = sample.Position.RelativeTo(_road.Origin);
                Vector3 forward = sample.Rotation * Vector3.forward;
                forward.y = 0f;
                direction = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;
            }
            else
            {
                from = to = GroundColor(_schedule.BiomeAt(distance));
            }

            Shader.SetGlobalColor(_colorAId, from);
            Shader.SetGlobalColor(_colorBId, to);
            Shader.SetGlobalVector(_planeId, new Vector4(point.x, point.y, point.z, Math.Max(1f, _schedule.BlendMeters)));
            Shader.SetGlobalVector(_directionId, direction);

            WorldPoint origin = _road.Origin;
            Shader.SetGlobalVector(_groundUvId, new Vector4((float)Repeat(origin.X), (float)Repeat(origin.Z), 0f, 0f));
        }

        private Color GroundColor(int biome)
        {
            return _pool.Set.Biomes[biome].GroundColor;
        }

        private static double Repeat(double value)
        {
            return value - Math.Floor(value / GroundUvPeriod) * GroundUvPeriod;
        }
    }
}
