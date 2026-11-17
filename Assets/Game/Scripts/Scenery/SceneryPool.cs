using System;
using System.Collections.Generic;
using System.Diagnostics;
using AF.Core;
using UnityEngine;
using VContainer;

namespace WrongLine.Scenery
{
    /// <summary>
    /// A root-scope singleton over the framework's pools, one pool per prop prefab (the road piece pool's pattern): instances
    /// wait under the root's <c>[Pools]</c> and outlive every scene. Spawned props are not parented under a scene object;
    /// whoever spawns them releases them, and <see cref="ReleaseAll"/> does it for a scope that ends.
    /// </summary>
    public sealed class SceneryPool : ISceneryPool, IDisposable
    {
        private readonly SceneryConfig _config;
        private readonly IPool<SceneryPropView>[] _pools;
        private readonly Dictionary<SceneryPropView, IPool<SceneryPropView>> _out = new Dictionary<SceneryPropView, IPool<SceneryPropView>>();
        private bool _warm;

        [Inject]
        public SceneryPool(IPools pools, SceneryCatalog catalog, SceneryConfig config)
        {
            config.Validate();
            _config = config;
            Set = ScenerySet.From(catalog);
            _pools = new IPool<SceneryPropView>[Set.Prefabs.Count];
            for (int i = 0; i < _pools.Length; i++)
            {
                _pools[i] = pools.For(Set.Prefabs[i]);
            }
        }

        public ScenerySet Set { get; }

        public int CountActive => _out.Count;

        public void Prewarm()
        {
            if (_warm)
            {
                return;
            }

            _warm = true;
            var watch = Stopwatch.StartNew();
            int made = 0;
            foreach (IPool<SceneryPropView> pool in _pools)
            {
                pool.Prewarm(_config.PrewarmPerProp);
                made += pool.CountInactive;
            }

            Log.Info($"[Scenery] Prewarmed {made} prop instances of {_pools.Length} prefabs in {watch.Elapsed.TotalMilliseconds:F1} ms.");
        }

        public SceneryPropView Spawn(int propId, Vector3 position, Quaternion rotation, float scale)
        {
            if (propId < 0 || propId >= _pools.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(propId), $"Prop id {propId} is not in the scenery set.");
            }

            IPool<SceneryPropView> pool = _pools[propId];
            SceneryPropView view = pool.Spawn(position, rotation);
            view.transform.localScale = new Vector3(scale, scale, scale);
            _out.Add(view, pool);
            return view;
        }

        public void Release(SceneryPropView view)
        {
            // A destroyed prop still compares equal to null; only a real null is a mistake.
            if (ReferenceEquals(view, null) || !_out.TryGetValue(view, out IPool<SceneryPropView> pool))
            {
                throw new ArgumentException("The prop is not out of this pool (released already, or not from it).", nameof(view));
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
            foreach (KeyValuePair<SceneryPropView, IPool<SceneryPropView>> pair in new List<KeyValuePair<SceneryPropView, IPool<SceneryPropView>>>(_out))
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
