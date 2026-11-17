using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using WrongLine.Driving;
using WrongLine.Road;
using WrongLine.World;

namespace WrongLine.Scenery
{
    /// <summary>
    /// The view side of the roadside: puts each road piece's props out from the pool when the piece comes into the scene and
    /// takes them back when it leaves. A piece gets its props once the piece after it is placed, so the planner sees the road
    /// on both sides of it. Positions are kept in double precision and placed again when the scene origin moves.
    /// </summary>
    internal sealed class SceneryStreamer : IStartable, ITickable, IDisposable, IRoadsideLamps
    {
        /// <summary>Mixed into the run seed, so the props do not repeat the road generator's numbers.</summary>
        private const ulong SeedSalt = 0x5CE7E3A1D0F2B4C9UL;

        /// <summary>Farthest a prop stands from the camera beyond the road ahead: the camera's lag plus the ground's width.</summary>
        private const float BoundsDistanceMargin = 150f;

        private readonly ISceneryPool _pool;
        private readonly IRoadFrame _road;
        private readonly BiomeSchedule _schedule;
        private readonly SceneryConfig _config;
        private readonly CurveWorldConfig _curve;
        private readonly RoadConfig _roadConfig;
        private readonly IRunSeed _seed;
        private readonly SceneryWindow _window = new SceneryWindow();
        private readonly List<int> _added = new List<int>();
        private readonly List<int> _removed = new List<int>();
        private readonly List<SceneryItem> _items = new List<SceneryItem>();
        private readonly Dictionary<int, List<Placed>> _props = new Dictionary<int, List<Placed>>();
        private readonly Stack<List<Placed>> _spareLists = new Stack<List<Placed>>();
        private readonly List<Transform> _lampHeads = new List<Transform>();
        private SceneryPlanner _planner;
        private WorldPoint _origin;

        public IReadOnlyList<Transform> Heads => _lampHeads;

        public SceneryStreamer(ISceneryPool pool, IRoadFrame road, BiomeSchedule schedule, SceneryConfig config, CurveWorldConfig curve, RoadConfig roadConfig, IRunSeed seed)
        {
            _pool = pool;
            _road = road;
            _schedule = schedule;
            _config = config;
            _curve = curve;
            _roadConfig = roadConfig;
            _seed = seed;
        }

        public void Start()
        {
            // Normally done in the menu; a run started straight from this scene does it here.
            _pool.Prewarm();
            _planner = new SceneryPlanner(_pool.Set.Rules, _schedule, _config.Ground, _seed.Value ^ SeedSalt);
            _origin = _road.Origin;
            Apply();
        }

        public void Tick()
        {
            Apply();
        }

        public void Dispose()
        {
            _pool.ReleaseAll();
            _props.Clear();
            _lampHeads.Clear();
        }

        private void Apply()
        {
            IRoadPath path = _road.Path;

            // Only pieces whose next piece is placed: the planner keeps props clear of the road that follows.
            int end = Math.Min(_road.LastActive + 1, path.EndIndex - 1);
            _window.Update(_road.FirstActive, end, _added, _removed);

            foreach (int index in _removed)
            {
                List<Placed> props = _props[index];
                foreach (Placed placed in props)
                {
                    if (placed.View.LightHead != null)
                    {
                        _lampHeads.Remove(placed.View.LightHead);
                    }

                    _pool.Release(placed.View);
                }

                props.Clear();
                _spareLists.Push(props);
                _props.Remove(index);
            }

            bool shifted = !SameOrigin(_origin, _road.Origin);
            _origin = _road.Origin;

            float margin = CurveBend.MaxDrop(_curve.Settings, _roadConfig.AheadMeters + BoundsDistanceMargin);
            foreach (int index in _added)
            {
                _items.Clear();
                _planner.Plan(path, index, _items);
                List<Placed> props = _spareLists.Count > 0 ? _spareLists.Pop() : new List<Placed>();
                foreach (SceneryItem item in _items)
                {
                    SceneryPropView view = _pool.Spawn(item.PropId, item.Position.RelativeTo(_origin), Quaternion.Euler(0f, item.YawDegrees, 0f), item.Scale);
                    view.ExpandBounds(margin);
                    if (view.LightHead != null)
                    {
                        _lampHeads.Add(view.LightHead);
                    }

                    props.Add(new Placed(view, item.Position));
                }

                _props.Add(index, props);
            }

            if (shifted)
            {
                // Placed again from the doubles, never nudged (the road streamer does the same).
                foreach (List<Placed> props in _props.Values)
                {
                    foreach (Placed placed in props)
                    {
                        placed.View.transform.position = placed.Position.RelativeTo(_origin);
                    }
                }
            }
        }

        private static bool SameOrigin(in WorldPoint a, in WorldPoint b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        private readonly struct Placed
        {
            public readonly SceneryPropView View;
            public readonly WorldPoint Position;

            public Placed(SceneryPropView view, in WorldPoint position)
            {
                View = view;
                Position = position;
            }
        }
    }
}
