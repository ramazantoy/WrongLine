using System;
using System.Collections.Generic;
using AF.Core;
using UnityEngine;

namespace WrongLine.Road.Tests
{
    /// <summary>
    /// Stands in for the framework's <see cref="IPools"/> and follows the contract the real one documents: an instance
    /// waits inactive, spawning activates it and calls <c>OnSpawned</c>, despawning calls <c>OnDespawned</c> and then
    /// deactivates it. The real pool is exercised in Play (see Game.md).
    /// </summary>
    internal sealed class FakeRoadPools : IPools
    {
        private readonly RoadTestKit _kit;

        public FakeRoadPools(RoadTestKit kit)
        {
            _kit = kit;
        }

        public Dictionary<RoadPieceView, FakeRoadPool> Pools { get; } = new Dictionary<RoadPieceView, FakeRoadPool>();

        public IPool<T> For<T>(T prefab) where T : Component
        {
            var view = prefab as RoadPieceView ?? throw new ArgumentException("Only road pieces are pooled here.");
            if (!Pools.TryGetValue(view, out FakeRoadPool pool))
            {
                pool = new FakeRoadPool(view, _kit);
                Pools.Add(view, pool);
            }

            return (IPool<T>)(object)pool;
        }
    }

    internal sealed class FakeRoadPool : IPool<RoadPieceView>
    {
        private readonly RoadTestKit _kit;
        private readonly Stack<RoadPieceView> _inactive = new Stack<RoadPieceView>();
        private readonly HashSet<RoadPieceView> _spawned = new HashSet<RoadPieceView>();

        public FakeRoadPool(RoadPieceView prefab, RoadTestKit kit)
        {
            Prefab = prefab;
            _kit = kit;
        }

        public RoadPieceView Prefab { get; }

        public int CountActive => _spawned.Count;

        public int CountInactive => _inactive.Count;

        public int PrewarmCalls { get; private set; }

        /// <summary>Instances ever made. A pool that leaks makes more of them as it is used.</summary>
        public int CreatedCount { get; private set; }

        public RoadPieceView Spawn(Transform parent = null)
        {
            return Spawn(Prefab.transform.position, Prefab.transform.rotation, parent);
        }

        public RoadPieceView Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            RoadPieceView view = _inactive.Count > 0 ? _inactive.Pop() : Create();
            view.transform.SetParent(parent, false);
            view.transform.SetPositionAndRotation(position, rotation);
            view.gameObject.SetActive(true);
            ((IPoolable)view).OnSpawned(default);
            _spawned.Add(view);
            return view;
        }

        public void Despawn(RoadPieceView instance)
        {
            if (!_spawned.Remove(instance))
            {
                throw new InvalidOperationException("Not spawned from this pool.");
            }

            ((IPoolable)instance).OnDespawned();
            instance.gameObject.SetActive(false);
            _inactive.Push(instance);
        }

        public void DespawnAll()
        {
            foreach (RoadPieceView view in new List<RoadPieceView>(_spawned))
            {
                Despawn(view);
            }
        }

        public void Prewarm(int count)
        {
            PrewarmCalls++;
            while (_inactive.Count < count)
            {
                _inactive.Push(Create());
            }
        }

        private RoadPieceView Create()
        {
            CreatedCount++;
            var go = _kit.Track(new GameObject($"{Prefab.name}(Clone)"));
            go.SetActive(false);
            return go.AddComponent<RoadPieceView>();
        }
    }
}
