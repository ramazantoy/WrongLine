using System;
using System.Collections.Generic;
using System.Diagnostics;
using AF.Core;
using UnityEngine;
using VContainer;

namespace WrongLine.Traffic
{
    /// <summary>
    /// A root-scope singleton over the framework's pools, one pool per car model (the road piece pool's pattern): instances wait
    /// under the root's <c>[Pools]</c> and outlive every scene. The three levels of detail live inside each instance, so a car
    /// changing level is one switch and never a trip through the pool. Spawned cars are not parented under a scene object;
    /// whoever spawns them releases them, and <see cref="ReleaseAll"/> does it for a scope that ends.
    /// </summary>
    public sealed class TrafficPool : ITrafficPool, IDisposable
    {
        private readonly TrafficCatalog _catalog;
        private readonly int _maxActive;
        private readonly IPool<TrafficCarView>[] _pools;
        private readonly TrafficModelSpec[] _models;
        private readonly Dictionary<TrafficCarView, IPool<TrafficCarView>> _out = new Dictionary<TrafficCarView, IPool<TrafficCarView>>();
        private bool _warm;

        [Inject]
        public TrafficPool(IPools pools, TrafficCatalog catalog, TrafficConfig config)
        {
            catalog.Validate();
            _catalog = catalog;
            _maxActive = config.Settings.Stream.MaxActive;

            TrafficCatalog.Entry[] entries = catalog.Models;
            _pools = new IPool<TrafficCarView>[entries.Length];
            _models = new TrafficModelSpec[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                _pools[i] = pools.For(entries[i].Prefab);
                _models[i] = new TrafficModelSpec(entries[i].Length, entries[i].Weight, entries[i].FrontAxle, entries[i].RearAxle, entries[i].HalfTrack);
            }
        }

        public IReadOnlyList<TrafficModelSpec> Models => _models;

        public int PaintCount => _catalog.Paints.Length;

        public int CountActive => _out.Count;

        public Material Paint(int index)
        {
            return _catalog.Paints[index];
        }

        public void Prewarm()
        {
            if (_warm)
            {
                return;
            }

            _warm = true;
            var watch = Stopwatch.StartNew();
            float total = 0f;
            foreach (TrafficModelSpec model in _models)
            {
                total += model.Weight;
            }

            int made = 0;
            for (int i = 0; i < _pools.Length; i++)
            {
                // The share of the most cars out at once this model gets, and a little more for a lucky draw.
                int count = Math.Min(_maxActive, (int)Math.Ceiling(_maxActive * _models[i].Weight / total) + 2);
                _pools[i].Prewarm(count);
                made += _pools[i].CountInactive;
            }

            Log.Info($"[Traffic] Prewarmed {made} car instances of {_pools.Length} models in {watch.Elapsed.TotalMilliseconds:F1} ms.");
        }

        public TrafficCarView Spawn(int model, Vector3 position, Quaternion rotation)
        {
            if (model < 0 || model >= _pools.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(model), $"Model {model} is not in the traffic catalog.");
            }

            IPool<TrafficCarView> pool = _pools[model];
            TrafficCarView view = pool.Spawn(position, rotation);
            _out.Add(view, pool);
            return view;
        }

        public void Release(TrafficCarView view)
        {
            // A destroyed car still compares equal to null; only a real null is a mistake.
            if (ReferenceEquals(view, null) || !_out.TryGetValue(view, out IPool<TrafficCarView> pool))
            {
                throw new ArgumentException("The car is not out of this pool (released already, or not from it).", nameof(view));
            }

            _out.Remove(view);
            if (view == null || !view.gameObject.scene.IsValid())
            {
                // Destroyed meanwhile, or the root's scene is already gone (stopping Play): see RoadPiecePool.Release.
                return;
            }

            pool.Despawn(view);
        }

        public void ReleaseAll()
        {
            foreach (KeyValuePair<TrafficCarView, IPool<TrafficCarView>> pair in new List<KeyValuePair<TrafficCarView, IPool<TrafficCarView>>>(_out))
            {
                Release(pair.Key);
            }
        }

        public void Dispose()
        {
            // The framework pools destroy their instances with the root scope.
            _out.Clear();
        }
    }
}
