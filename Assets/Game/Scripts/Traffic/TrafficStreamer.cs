using System;
using System.Collections.Generic;
using AF.Core;
using UnityEngine;
using VContainer.Unity;
using WrongLine.Car;
using WrongLine.Driving;
using WrongLine.Road;
using WrongLine.World;

namespace WrongLine.Traffic
{
    /// <summary>
    /// The view side of the traffic: puts each car out from the pool when it comes into range and takes it back when it is behind
    /// the player, places the cars on the road every frame and picks their level of detail. The rules live in
    /// <see cref="TrafficRun"/>, <see cref="TrafficPlanner"/> and <see cref="TrafficLodRule"/>. A car's place is its distance
    /// along the road and its lane (in double precision), so the scene origin moving needs nothing special.
    /// </summary>
    internal sealed class TrafficStreamer : IStartable, ITickable, IDisposable, ITrafficFrame
    {
        /// <summary>Mixed into the run seed, so the traffic does not repeat the road generator's numbers.</summary>
        private const ulong SeedSalt = 0x7AFF1C5EED5A17E5UL;

        /// <summary>Farthest a car stands from the camera beyond the spawn range: the camera's lag plus some room.</summary>
        private const float BoundsDistanceMargin = 50f;

        private static readonly Quaternion Oncoming = Quaternion.Euler(0f, 180f, 0f);

        private readonly ITrafficPool _pool;
        private readonly IRoadFrame _road;
        private readonly IGameClock _clock;
        private readonly TrafficConfig _config;
        private readonly CarConfig _car;
        private readonly RoadConfig _roadConfig;
        private readonly CurveWorldConfig _curve;
        private readonly IRunSeed _seed;
        private readonly Dictionary<int, TrafficCarView> _views = new Dictionary<int, TrafficCarView>();
        private TrafficRun _run;
        private TrafficLodRule _lod;

        public TrafficStreamer(ITrafficPool pool, IRoadFrame road, IGameClock clock, TrafficConfig config, CarConfig car, RoadConfig roadConfig, CurveWorldConfig curve, IRunSeed seed)
        {
            _pool = pool;
            _road = road;
            _clock = clock;
            _config = config;
            _car = car;
            _roadConfig = roadConfig;
            _curve = curve;
            _seed = seed;
        }

        public IReadOnlyList<TrafficCarPlan> Active => _run != null ? _run.Active : Array.Empty<TrafficCarPlan>();

        public double Time => _run != null ? _run.Time : 0.0;

        public void Start()
        {
            if (!_config.Enabled)
            {
                return;
            }

            TrafficSettings settings = _config.Settings;
            settings.Validate(_roadConfig.BehindMeters, _roadConfig.AheadMeters);

            // Normally done in the menu; a run started straight from this scene does it here.
            _pool.Prewarm();

            var profile = new PlayerSpeedProfile(_car.Settings.Speed);
            var planner = new TrafficPlanner(_pool.Models, settings, profile, _seed.Value ^ SeedSalt, _car.Settings.Lateral.StartLane, _pool.PaintCount);
            _run = new TrafficRun(planner, settings);
            _lod = new TrafficLodRule(settings.Lod);
            Apply(0f);
        }

        public void Tick()
        {
            if (_run == null)
            {
                return;
            }

            Apply(_clock.DeltaTime);
        }

        public void Dispose()
        {
            _pool.ReleaseAll();
            _views.Clear();
        }

        private void Apply(float deltaTime)
        {
            IRoadPath path = _road.Path;
            double player = _road.Distance;
            _run.Advance(deltaTime, player, path[path.FirstIndex].EntryDistance);

            foreach (int id in _run.Despawned)
            {
                _pool.Release(_views[id]);
                _views.Remove(id);
            }

            WorldPoint origin = _road.Origin;
            float margin = CurveBend.MaxDrop(_curve.Settings, _config.Settings.Stream.SpawnAheadMeters + BoundsDistanceMargin);
            foreach (TrafficCarPlan plan in _run.Spawned)
            {
                TrafficCarView view = _pool.Spawn(plan.Model, Vector3.zero, Quaternion.identity);
                view.SetPaint(_pool.Paint(plan.Paint));
                view.ExpandBounds(margin);
                _views.Add(plan.Id, view);
            }

            foreach (TrafficCarPlan plan in _run.Active)
            {
                double distance = plan.DistanceAt(_run.Time);
                RoadSample sample = RoadSampler.At(path, distance, RoadSection.LaneCenterX(plan.Lane));
                TrafficCarView view = _views[plan.Id];
                view.transform.SetPositionAndRotation(sample.Position.RelativeTo(origin), plan.Speed < 0.0 ? sample.Rotation * Oncoming : sample.Rotation);
                view.ShowLevel(_lod.Evaluate((float)Math.Abs(distance - player), view.Level));
            }
        }
    }
}
