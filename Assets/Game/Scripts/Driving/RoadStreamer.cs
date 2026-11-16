using System;
using System.Collections.Generic;
using AF.Core;
using MessagePipe;
using VContainer.Unity;
using WrongLine.Road;
using WrongLine.World;

namespace WrongLine.Driving
{
    /// <summary>
    /// The view side of a run: brings road pieces in from the pool and sends them back as the follower advances, places the
    /// follower on the road, and tells the camera when the scene origin moves. The rules live in <see cref="RoadRun"/>.
    /// </summary>
    internal sealed class RoadStreamer : IStartable, ITickable, IDisposable
    {
        private readonly IRoadPiecePool _pool;
        private readonly RoadConfig _road;
        private readonly CurveWorldConfig _curve;
        private readonly FollowerConfig _follower;
        private readonly DummyFollowerView _followerView;
        private readonly IGameClock _clock;
        private const float BoundsDistanceMargin = 50f; // The camera sits behind the follower; pieces ahead are this much further from it than AheadMeters.

        private readonly IPublisher<RoadOriginShifted> _shifted;
        private readonly IRoadDebugHud _hud;
        private readonly Dictionary<int, RoadPieceView> _views = new Dictionary<int, RoadPieceView>();
        private RoadRun _run;

        public RoadStreamer(IRoadPiecePool pool, RoadConfig road, CurveWorldConfig curve, FollowerConfig follower, DummyFollowerView followerView, IGameClock clock, IPublisher<RoadOriginShifted> shifted, IRoadDebugHud hud)
        {
            _curve = curve;
            _pool = pool;
            _road = road;
            _follower = follower;
            _followerView = followerView;
            _clock = clock;
            _shifted = shifted;
            _hud = hud;
        }

        public void Start()
        {
            // Normally done in the menu; a run started straight from this scene does it here.
            _pool.Prewarm();

            ulong seed = RoadSeed.Resolve(_road.Seed, unchecked((ulong)DateTime.UtcNow.Ticks));
            Log.Info($"[Road] Run seed {seed}.");
            _run = new RoadRun(_pool.Set.Infos, _road.Generator, seed, _road.AheadMeters, _road.BehindMeters, _road.OriginShiftMeters);
            _run.Advance(0.0);
            Apply();
        }

        public void Tick()
        {
            _run.Advance(_follower.SpeedMetersPerSecond * _clock.DeltaTime);
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
            }

            RoadCursor cursor = _run.Cursor;
            _followerView.Place(cursor.WorldPosition.RelativeTo(_run.Origin), cursor.WorldRotation);

            if (_run.OriginShifted)
            {
                _shifted.Publish(new RoadOriginShifted(_run.OriginShiftVector));
            }

            PlacedPiece current = cursor.Piece;
            _hud.Show(new RoadDebugInfo(cursor.Distance, current.Index, current.Piece.Name, cursor.LocalS, current.Piece.Length, _run.Seed, _follower.SpeedMetersPerSecond, _views.Count, _run.ShiftCount));
        }
    }
}
