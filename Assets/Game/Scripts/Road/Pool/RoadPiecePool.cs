using System;
using System.Collections.Generic;
using System.Diagnostics;
using AF.Core;
using UnityEngine;
using VContainer;

namespace WrongLine.Road
{
    /// <summary>
    /// A root-scope singleton over the framework's pools: asking <see cref="IPools"/> from the root gives the root's pools,
    /// whose instances wait inactive under the root's <c>[Pools]</c> and outlive every scene. One pool per piece prefab.
    /// Spawned pieces are not parented under a scene object (they would die with the scene); whoever spawns them releases
    /// them, and <see cref="ReleaseAll"/> does it for a scope that ends.
    /// </summary>
    public sealed class RoadPiecePool : IRoadPiecePool, IDisposable
    {
        private readonly RoadConfig _config;
        private readonly IPool<RoadPieceView>[] _pools;
        private readonly Dictionary<RoadPieceView, IPool<RoadPieceView>> _out = new Dictionary<RoadPieceView, IPool<RoadPieceView>>();
        private bool _warm;

        [Inject]
        public RoadPiecePool(IPools pools, RoadCatalog catalog, RoadConfig config)
        {
            config.Validate();
            _config = config;
            Set = RoadPieceSet.From(catalog);
            _pools = new IPool<RoadPieceView>[Set.Prefabs.Count];
            for (int i = 0; i < _pools.Length; i++)
            {
                _pools[i] = pools.For(Set.Prefabs[i]);
            }
        }

        public RoadPieceSet Set { get; }

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
            for (int i = 0; i < _pools.Length; i++)
            {
                // One start piece is all a run ever needs.
                int count = Set.Infos[i].IsStart ? Math.Min(1, _config.PrewarmPerPiece) : _config.PrewarmPerPiece;
                _pools[i].Prewarm(count);
                made += _pools[i].CountInactive;
            }

            Log.Info($"[Road] Prewarmed {made} piece instances of {_pools.Length} prefabs in {watch.Elapsed.TotalMilliseconds:F1} ms.");
        }

        public RoadPieceView Spawn(int placedIndex, RoadPieceInfo piece, Vector3 position, Quaternion rotation)
        {
            if (piece.Id < 0 || piece.Id >= _pools.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(piece), $"Piece id {piece.Id} is not in the catalog.");
            }

            IPool<RoadPieceView> pool = _pools[piece.Id];
            RoadPieceView view = pool.Spawn(position, rotation);
            view.Bind(placedIndex);
            _out.Add(view, pool);
            return view;
        }

        public void Release(RoadPieceView view)
        {
            // A destroyed piece still compares equal to null; only a real null is a mistake.
            if (ReferenceEquals(view, null) || !_out.TryGetValue(view, out IPool<RoadPieceView> pool))
            {
                throw new ArgumentException("The piece is not out of this pool (released already, or not from it).", nameof(view));
            }

            _out.Remove(view);
            if (view == null || !view.gameObject.scene.IsValid())
            {
                // Destroyed meanwhile, or its scene is already gone: stopping Play takes the root's scene away before its
                // objects are destroyed, and putting a piece back then makes Unity assert inside SetParent. The root's
                // pools destroy the instance themselves; nothing is left to give back.
                return;
            }

            pool.Despawn(view);
        }

        public void ReleaseAll()
        {
            foreach (KeyValuePair<RoadPieceView, IPool<RoadPieceView>> pair in new List<KeyValuePair<RoadPieceView, IPool<RoadPieceView>>>(_out))
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
