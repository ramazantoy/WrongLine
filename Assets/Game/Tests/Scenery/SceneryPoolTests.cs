using System;
using System.Collections.Generic;
using System.Linq;
using AF.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WrongLine.Road.Tests;
using Object = UnityEngine.Object;

namespace WrongLine.Scenery.Tests
{
    public class SceneryPoolTests
    {
        private RoadTestKit _kit;
        private FakeSceneryPools _pools;
        private SceneryPropView _lamp;
        private SceneryPropView _tree;
        private SceneryPropView _cactus;

        [SetUp]
        public void SetUpPool()
        {
            _kit = new RoadTestKit();
            _pools = new FakeSceneryPools(_kit);
            _lamp = MakePrefab("Shared_StreetLamp");
            _tree = MakePrefab("Meadow_Tree");
            _cactus = MakePrefab("Desert_Cactus");
        }

        [TearDown]
        public void TearDownPool()
        {
            _kit.Dispose();
        }

        private SceneryPropView MakePrefab(string name)
        {
            var go = _kit.Track(new GameObject(name));
            return go.AddComponent<SceneryPropView>();
        }

        private SceneryCatalog MakeCatalog(SceneryPropView[] shared, params (string Name, SceneryPropView[] Props)[] biomes)
        {
            var catalog = _kit.Track(ScriptableObject.CreateInstance<SceneryCatalog>());
            var serialized = new SerializedObject(catalog);
            Fill(serialized.FindProperty("_shared"), shared);
            SerializedProperty list = serialized.FindProperty("_biomes");
            list.arraySize = biomes.Length;
            for (int i = 0; i < biomes.Length; i++)
            {
                SerializedProperty biome = list.GetArrayElementAtIndex(i);
                biome.FindPropertyRelative("_name").stringValue = biomes[i].Name;
                Fill(biome.FindPropertyRelative("_props"), biomes[i].Props);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        private static void Fill(SerializedProperty list, SceneryPropView[] prefabs)
        {
            list.arraySize = prefabs.Length;
            for (int i = 0; i < prefabs.Length; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_name").stringValue = prefabs[i] != null ? prefabs[i].name : "Missing";
                entry.FindPropertyRelative("_prefab").objectReferenceValue = prefabs[i];
                entry.FindPropertyRelative("_scaleRange").vector2Value = Vector2.one;
            }
        }

        private SceneryConfig MakeConfig(int prewarm = 4)
        {
            var config = _kit.Track(ScriptableObject.CreateInstance<SceneryConfig>());
            var serialized = new SerializedObject(config);
            serialized.FindProperty("_prewarmPerProp").intValue = prewarm;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private SceneryPool MakePool()
        {
            return new SceneryPool(_pools, MakeCatalog(new[] { _lamp }, ("Meadow", new[] { _tree }), ("Desert", new[] { _cactus })), MakeConfig());
        }

        [Test]
        public void TheSet_NumbersSharedPropsFirst_ThenEachBiomesInOrder_AndKnowsTheirBiome()
        {
            SceneryPool pool = MakePool();

            CollectionAssert.AreEqual(new[] { _lamp, _tree, _cactus }, pool.Set.Prefabs.ToArray());
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, pool.Set.Rules.Select(rule => rule.Id).ToArray());
            CollectionAssert.AreEqual(new[] { PropRule.Shared, 0, 1 }, pool.Set.Rules.Select(rule => rule.Biome).ToArray());
            Assert.AreEqual(2, pool.Set.Biomes.Count);
        }

        [Test]
        public void Prewarming_MakesTheConfiguredNumberOfEach_Once()
        {
            SceneryPool pool = MakePool();
            Assert.AreEqual(0, _pools.Pools.Values.Sum(p => p.CreatedCount), "Constructing makes nothing.");

            pool.Prewarm();
            pool.Prewarm();

            Assert.IsTrue(_pools.Pools.Values.All(p => p.CountInactive == 4 && p.PrewarmCalls == 1));
        }

        [Test]
        public void Spawn_PlacesTurnsAndSizesTheProp_AndReleaseSendsItBackClean()
        {
            SceneryPool pool = MakePool();
            pool.Prewarm();

            SceneryPropView view = pool.Spawn(1, new Vector3(5f, 1f, 9f), Quaternion.Euler(0f, 70f, 0f), 1.4f);

            Assert.IsTrue(view.gameObject.activeSelf);
            Assert.AreEqual(new Vector3(5f, 1f, 9f), view.transform.position);
            Assert.AreEqual(new Vector3(1.4f, 1.4f, 1.4f), view.transform.localScale);
            Assert.AreEqual(1, pool.CountActive);
            Assert.AreEqual(3, _pools.Pools[_tree].CountInactive);

            pool.Release(view);

            Assert.IsFalse(view.gameObject.activeSelf);
            Assert.AreEqual(Vector3.zero, view.transform.position);
            Assert.AreEqual(Vector3.one, view.transform.localScale, "No scale kept.");
            Assert.AreEqual(0, pool.CountActive);
        }

        [Test]
        public void ReleasingTwice_AStranger_OrNull_IsRejected_AndADestroyedPropIsLetGo()
        {
            SceneryPool pool = MakePool();
            SceneryPropView view = pool.Spawn(0, Vector3.zero, Quaternion.identity, 1f);
            pool.Release(view);

            Assert.Throws<ArgumentException>(() => pool.Release(view));
            Assert.Throws<ArgumentException>(() => pool.Release(_tree));
            Assert.Throws<ArgumentException>(() => pool.Release(null));

            SceneryPropView doomed = pool.Spawn(2, Vector3.zero, Quaternion.identity, 1f);
            Object.DestroyImmediate(doomed.gameObject);
            Assert.DoesNotThrow(() => pool.ReleaseAll());
            Assert.AreEqual(0, pool.CountActive);
        }

        [Test]
        public void ReleaseAll_TakesEverythingBack()
        {
            SceneryPool pool = MakePool();
            for (int i = 0; i < 9; i++)
            {
                pool.Spawn(i % 3, new Vector3(i, 0f, 0f), Quaternion.identity, 1f);
            }

            pool.ReleaseAll();

            Assert.AreEqual(0, pool.CountActive);
            Assert.IsTrue(_pools.Pools.Values.All(p => p.CountActive == 0));
        }

        [Test]
        public void AnUnknownProp_ACatalogWithoutBiomes_OrAPropWithoutPrefab_IsRejected()
        {
            SceneryPool pool = MakePool();
            Assert.Throws<ArgumentOutOfRangeException>(() => pool.Spawn(3, Vector3.zero, Quaternion.identity, 1f));

            Assert.Throws<ArgumentException>(() => new SceneryPool(_pools, MakeCatalog(new[] { _lamp }), MakeConfig()));
            Assert.Throws<ArgumentException>(() => new SceneryPool(_pools, MakeCatalog(new SceneryPropView[0], ("Meadow", new SceneryPropView[] { null })), MakeConfig()));
        }

        [Test]
        public void ExpandBounds_GrowsEveryMeshInTheProp_FromItsOwnBounds()
        {
            var mesh = _kit.Track(new Mesh());
            mesh.vertices = new[] { new Vector3(-1f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 3f, 0f) };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateBounds();
            var part = new GameObject("Crown");
            part.transform.SetParent(_tree.transform, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();

            _tree.ExpandBounds(6f);
            _tree.ExpandBounds(6f);

            Assert.AreEqual(mesh.bounds.extents + Vector3.one * 6f, renderer.localBounds.extents);
        }
    }

    /// <summary>The framework pool's contract for props (see <see cref="FakeRoadPools"/>).</summary>
    internal sealed class FakeSceneryPools : IPools
    {
        private readonly RoadTestKit _kit;

        public FakeSceneryPools(RoadTestKit kit)
        {
            _kit = kit;
        }

        public Dictionary<SceneryPropView, FakeSceneryPool> Pools { get; } = new Dictionary<SceneryPropView, FakeSceneryPool>();

        public IPool<T> For<T>(T prefab) where T : Component
        {
            var view = prefab as SceneryPropView ?? throw new ArgumentException("Only props are pooled here.");
            if (!Pools.TryGetValue(view, out FakeSceneryPool pool))
            {
                pool = new FakeSceneryPool(view, _kit);
                Pools.Add(view, pool);
            }

            return (IPool<T>)(object)pool;
        }
    }

    internal sealed class FakeSceneryPool : IPool<SceneryPropView>
    {
        private readonly RoadTestKit _kit;
        private readonly Stack<SceneryPropView> _inactive = new Stack<SceneryPropView>();
        private readonly HashSet<SceneryPropView> _spawned = new HashSet<SceneryPropView>();

        public FakeSceneryPool(SceneryPropView prefab, RoadTestKit kit)
        {
            Prefab = prefab;
            _kit = kit;
        }

        public SceneryPropView Prefab { get; }

        public int CountActive => _spawned.Count;

        public int CountInactive => _inactive.Count;

        public int PrewarmCalls { get; private set; }

        public int CreatedCount { get; private set; }

        public SceneryPropView Spawn(Transform parent = null)
        {
            return Spawn(Vector3.zero, Quaternion.identity, parent);
        }

        public SceneryPropView Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            SceneryPropView view = _inactive.Count > 0 ? _inactive.Pop() : Create();
            view.transform.SetParent(parent, false);
            view.transform.SetPositionAndRotation(position, rotation);
            view.gameObject.SetActive(true);
            ((IPoolable)view).OnSpawned(default);
            _spawned.Add(view);
            return view;
        }

        public void Despawn(SceneryPropView instance)
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
            foreach (SceneryPropView view in new List<SceneryPropView>(_spawned))
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

        private SceneryPropView Create()
        {
            CreatedCount++;
            var go = _kit.Track(new GameObject($"{Prefab.name}(Clone)"));
            go.SetActive(false);
            return go.AddComponent<SceneryPropView>();
        }
    }
}
