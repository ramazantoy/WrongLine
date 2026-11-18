using System;
using System.Collections.Generic;
using AF.Core;
using MessagePipe;
using VContainer.Unity;
using WrongLine.Car;
using WrongLine.Road;
using WrongLine.World;

namespace WrongLine.Driving
{
    /// <summary>
    /// The view side of a run: brings road pieces in from the pool and sends them back as the car advances, and tells the others
    /// when the scene origin moves. The rules live in <see cref="RoadRun"/>; the car and the camera read where the car is
    /// through <see cref="IRoadFrame"/>.
    /// </summary>
    internal sealed class RoadStreamer : IStartable, ITickable, IDisposable, IRoadFrame
    {
        private readonly IRoadPiecePool _pool;
        private readonly IRunSeed _seed;
        private readonly RoadConfig _road;
        private readonly CurveWorldConfig _curve;
        private readonly ICarMotion _motion;
        private const float BoundsDistanceMargin = 50f; // The camera sits behind the car; pieces ahead are this much further from it than AheadMeters.

        private readonly IPublisher<RoadOriginShifted> _shifted;
        private readonly IRoadDebugHud _hud;
        private readonly Dictionary<int, RoadPieceView> _views = new Dictionary<int, RoadPieceView>();
        private RoadRun _run;
        private double _lastCovered;

        public RoadStreamer(IRoadPiecePool pool, RoadConfig road, CurveWorldConfig curve, ICarMotion motion, IPublisher<RoadOriginShifted> shifted, IRoadDebugHud hud, IRunSeed seed)
        {
            _seed = seed;
            _curve = curve;
            _pool = pool;
            _road = road;
            _motion = motion;
            _shifted = shifted;
            _hud = hud;
        }

        public IRoadPath Path => _run.Path;

        public double Distance => _run.Cursor.Distance;

        public WorldPoint Origin => _run.Origin;

        public int FirstActive => _run.FirstActive;

        public int LastActive => _run.LastActive;

        public void Start()
        {
            // Normally done in the menu; a run started straight from this scene does it here.
            _pool.Prewarm();

            _run = new RoadRun(_pool.Set.Infos, _road.Generator, _seed.Value,_road.AheadMeters, _road.BehindMeters, _road.OriginShiftMeters);
            _run.Advance(0.0);
            Apply();
        }

        public void Tick()
        {
            // How far the car has got comes from its speed profile (the traffic is laid out for the same numbers), not from adding up speed × frame time.
            double covered = _motion.Distance;
            _run.Advance(covered - _lastCovered);
            _lastCovered = covered;
            Apply();
        }

        public void Dispose()
        {
            _pool.ReleaseAll();
            _views.Clear();
        }

        private void Apply()
        {
            foreach (int index in _run.Despawned)
            {
                _pool.Release(_views[index]);
                _views.Remove(index);
            }

            foreach (int index in _run.Spawned)
            {
                PlacedPiece placed = _run.Path[index];
                RoadPieceView view = _pool.Spawn(index, placed.Piece, RoadPlacement.Position(placed, _run.Origin), RoadPlacement.Rotation(placed));
                view.ExpandBounds(CurveBend.MaxDrop(_curve.Settings, _road.AheadMeters + BoundsDistanceMargin));
                _views.Add(index, view);
            }

            if (_run.OriginShifted)
            {
                // Placed again from the doubles, never nudged: float nudges would slowly pull the pieces apart.
                foreach (KeyValuePair<int, RoadPieceView> pair in _views)
                {
                    pair.Value.transform.position = RoadPlacement.Position(_run.Path[pair.Key], _run.Origin);
                }

                _shifted.Publish(new RoadOriginShifted(_run.OriginShiftVector));
            }

            RoadCursor cursor = _run.Cursor;
            PlacedPiece current = cursor.Piece;
            string signal = _motion.Signal == SignalDirection.None ? "off" : _motion.Signal.ToString().ToLowerInvariant();
            _hud.Show(new RoadDebugInfo(cursor.Distance, current.Index, current.Piece.Name, cursor.LocalS, current.Piece.Length, _run.Seed, _motion.Speed, _views.Count, _run.ShiftCount, _motion.X, _motion.Lane, signal));
        }
    }
}
