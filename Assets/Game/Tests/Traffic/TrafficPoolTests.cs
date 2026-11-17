using System;
using System.Collections.Generic;
using System.Linq;
using AF.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WrongLine.Road.Tests;
using Object = UnityEngine.Object;

namespace WrongLine.Traffic.Tests
{
    public class TrafficPoolTests
    {
        private RoadTestKit _kit;
        private FakeTrafficPools _pools;
        private TrafficCarView _small;
        private TrafficCarView _big;
        private Material _paint;

        [SetUp]
        public void SetUpPool()
        {
            _kit = new RoadTestKit();
            _pools = new FakeTrafficPools(_kit);
            _small = FakeTrafficPools.MakeView(_kit, "Small");
            _big = FakeTrafficPools.MakeView(_kit, "Big");
            _paint = _kit.Track(new Material(Shader.Find("Sprites/Default")));
        }

        [TearDown]
        public void TearDownPool()
        {
            _kit.Dispose();
        }

        private TrafficCatalog MakeCatalog(Material[] paints, params (TrafficCarView Prefab, float Length, float Weight)[] models)
        {
            var catalog = _kit.Track(ScriptableObject.CreateInstance<TrafficCatalog>());
            var serialized = new SerializedObject(catalog);
            SerializedProperty list = serialized.FindProperty("_models");
            list.arraySize = models.Length;
            for (int i = 0; i < models.Length; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_name").stringValue = $"Model{i}";
                entry.FindPropertyRelative("_prefab").objectReferenceValue = models[i].Prefab;
                entry.FindPropertyRelative("_length").floatValue = models[i].Length;
                entry.FindPropertyRelative("_weight").floatValue = models[i].Weight;
            }

            SerializedProperty paintList = serialized.FindProperty("_paints");
            paintList.arraySize = paints.Length;
            for (int i = 0; i < paints.Length; i++)
            {
                paintList.GetArrayElementAtIndex(i).objectReferenceValue = paints[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        private TrafficPool MakePool()
        {
            var config = _kit.Track(ScriptableObject.CreateInstance<TrafficConfig>());
            return new TrafficPool(_pools, MakeCatalog(new[] { _paint }, (_small, 3.8f, 3f), (_big, 7.5f, 1f)), config);
        }

        [Test]
        public void TheModels_KeepCatalogOrder_AndCarryLengthAndWeight()
        {
            TrafficPool pool = MakePool();

            Assert.AreEqual(2, pool.Models.Count);
            Assert.AreEqual(3.8f, pool.Models[0].Length);
            Assert.AreEqual(3f, pool.Models[0].Weight);
            Assert.AreEqual(7.5f, pool.Models[1].Length);
            Assert.AreEqual(1, pool.PaintCount);
            Assert.AreSame(_paint, pool.Paint(0));
        }

        [Test]
        public void Prewarming_MakesEachModelsShareOfTheMostCarsOut_Once()
        {
            TrafficPool pool = MakePool();

            pool.Prewarm();
            pool.Prewarm();

            // 24 at most: small 3/4 of it plus 2, big 1/4 of it plus 2.
            Assert.AreEqual(20, _pools.Pools[_small].CountInactive);
            Assert.AreEqual(8, _pools.Pools[_big].CountInactive);
            Assert.IsTrue(_pools.Pools.Values.All(p => p.PrewarmCalls == 1));
        }

        [Test]
        public void Spawn_PlacesTheCar_AndReleaseSendsItBackClean()
        {
            TrafficPool pool = MakePool();
            pool.Prewarm();

            TrafficCarView view = pool.Spawn(1, new Vector3(4f, 1f, 9f), Quaternion.Euler(0f, 180f, 0f));
            view.ShowLevel(1);

            Assert.IsTrue(view.gameObject.activeSelf);
            Assert.AreEqual(new Vector3(4f, 1f, 9f), view.transform.position);
            Assert.AreEqual(1, pool.CountActive);

            pool.Release(view);

            Assert.IsFalse(view.gameObject.activeSelf);
            Assert.AreEqual(Vector3.zero, view.transform.position);
            Assert.AreEqual(TrafficLodRule.None, view.Level, "No level kept.");
            Assert.AreEqual(0, view.GetComponentsInChildren<Transform>().Count(t => t != view.transform && t.gameObject.activeSelf), "No level left on.");
            Assert.AreEqual(0, pool.CountActive);
        }

        [Test]
        public void ReleasingTwice_AStranger_OrNull_IsRejected_AndADestroyedCarIsLetGo()
        {
            TrafficPool pool = MakePool();
            TrafficCarView view = pool.Spawn(0, Vector3.zero, Quaternion.identity);
            pool.Release(view);

            Assert.Throws<ArgumentException>(() => pool.Release(view));
            Assert.Throws<ArgumentException>(() => pool.Release(_big));
            Assert.Throws<ArgumentException>(() => pool.Release(null));

            TrafficCarView doomed = pool.Spawn(1, Vector3.zero, Quaternion.identity);
            Object.DestroyImmediate(doomed.gameObject);
            Assert.DoesNotThrow(() => pool.ReleaseAll());
            Assert.AreEqual(0, pool.CountActive);
        }

        [Test]
        public void ReleaseAll_TakesEveryCarBack()
        {
            TrafficPool pool = MakePool();
            for (int i = 0; i < 6; i++)
            {
                pool.Spawn(i % 2, new Vector3(i, 0f, 0f), Quaternion.identity);
            }

            pool.ReleaseAll();

            Assert.AreEqual(0, pool.CountActive);
            Assert.IsTrue(_pools.Pools.Values.All(p => p.CountActive == 0));
        }

        [Test]
        public void AnUnknownModel_OrABrokenCatalog_IsRejected()
        {
            TrafficPool pool = MakePool();
            var config = _kit.Track(ScriptableObject.CreateInstance<TrafficConfig>());

            Assert.Throws<ArgumentOutOfRangeException>(() => pool.Spawn(2, Vector3.zero, Quaternion.identity));
            Assert.Throws<ArgumentException>(() => new TrafficPool(_pools, MakeCatalog(new Material[0], (_small, 3.8f, 1f)), config), "no paint");
            Assert.Throws<ArgumentException>(() => new TrafficPool(_pools, MakeCatalog(new[] { _paint }), config), "no model");
            Assert.Throws<ArgumentException>(() => new TrafficPool(_pools, MakeCatalog(new[] { _paint }, (null, 3.8f, 1f)), config), "no prefab");
        }

        [Test]
        public void ShowLevel_SwitchesOneLevelOn_AndNothingChangesWhenAskedForTheSameOne()
        {
            TrafficCarView view = FakeTrafficPools.MakeView(_kit, "Car");
            GameObject[] levels = Enumerable.Range(0, 3).Select(i => view.transform.GetChild(i).gameObject).ToArray();

            view.ShowLevel(2);
            CollectionAssert.AreEqual(new[] { false, false, true }, levels.Select(l => l.activeSelf).ToArray());

            view.ShowLevel(0);
            CollectionAssert.AreEqual(new[] { true, false, false }, levels.Select(l => l.activeSelf).ToArray());
            Assert.AreEqual(0, view.Level);
        }

        [Test]
        public void SetPaint_ChangesEveryMeshOfTheCar_AndOnlyTheirSharedMaterial()
        {
            TrafficCarView view = FakeTrafficPools.MakeView(_kit, "Car");
            var other = _kit.Track(new Material(Shader.Find("Sprites/Default")));

            view.SetPaint(other);

            MeshRenderer[] renderers = view.GetComponentsInChildren<MeshRenderer>(true);
            MeshRenderer[] bodies = renderers.Where(r => r.name != "Blob").ToArray();
            Assert.AreEqual(3, bodies.Length);
            Assert.IsTrue(bodies.All(r => r.sharedMaterial == other));
            Assert.AreNotSame(other, renderers.Single(r => r.name == "Blob").sharedMaterial, "The contact shadow keeps its own material.");
        }

        [Test]
        public void TheContactShadow_ShowsFromTheMiddleLevelOn_AndGoesWithTheCar()
        {
            TrafficCarView view = FakeTrafficPools.MakeView(_kit, "Car");
            GameObject blob = view.transform.Find("Blob").gameObject;

            view.ShowLevel(0);
            Assert.IsFalse(blob.activeSelf, "The near level has a real shadow.");

            view.ShowLevel(1);
            Assert.IsTrue(blob.activeSelf);

            view.ShowLevel(2);
            Assert.IsTrue(blob.activeSelf);

            ((IPoolable)view).OnDespawned();
            Assert.IsFalse(blob.activeSelf, "None left behind in the pool.");
        }

        [Test]
        public void ExpandBounds_GrowsEveryMeshInTheCar_FromItsOwnBounds()
        {
            TrafficCarView view = FakeTrafficPools.MakeView(_kit, "Car");
            MeshRenderer renderer = view.GetComponentsInChildren<MeshRenderer>(true)[0];
            Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;

            view.ExpandBounds(6f);
            view.ExpandBounds(6f);

            Assert.AreEqual(mesh.bounds.extents + Vector3.one * 6f, renderer.localBounds.extents);
        }
    }

    /// <summary>The framework pool's contract for traffic cars (see <see cref="FakeRoadPools"/>).</summary>
    internal sealed class FakeTrafficPools : IPools
    {
        private readonly RoadTestKit _kit;

        public FakeTrafficPools(RoadTestKit kit)
        {
            _kit = kit;
        }

        public Dictionary<TrafficCarView, FakeTrafficPool> Pools { get; } = new Dictionary<TrafficCarView, FakeTrafficPool>();

        /// <summary>A car prefab stand-in: the root, with three level children each holding one mesh.</summary>
        public static TrafficCarView MakeView(RoadTestKit kit, string name)
        {
            var root = kit.Track(new GameObject(name));
            var levels = new GameObject[TrafficCarView.LevelCount];
            for (int i = 0; i < levels.Length; i++)
            {
                var mesh = kit.Track(new Mesh());
                mesh.vertices = new[] { new Vector3(-1f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 2f, 0f) };
                mesh.triangles = new[] { 0, 1, 2 };
                mesh.RecalculateBounds();
                var level = new GameObject($"{name}_LOD{i}");
                level.transform.SetParent(root.transform, false);
                level.AddComponent<MeshFilter>().sharedMesh = mesh;
                level.AddComponent<MeshRenderer>();
                level.SetActive(false);
                levels[i] = level;
            }

            var blob = new GameObject("Blob");
            blob.transform.SetParent(root.transform, false);
            blob.AddComponent<MeshFilter>();
            blob.AddComponent<MeshRenderer>();
            blob.SetActive(false);

            var view = root.AddComponent<TrafficCarView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("_blob").objectReferenceValue = blob;
            SerializedProperty list = serialized.FindProperty("_levels");
            list.arraySize = levels.Length;
            for (int i = 0; i < levels.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        public IPool<T> For<T>(T prefab) where T : Component
        {
            var view = prefab as TrafficCarView ?? throw new ArgumentException("Only traffic cars are pooled here.");
            if (!Pools.TryGetValue(view, out FakeTrafficPool pool))
            {
                pool = new FakeTrafficPool(view, _kit);
                Pools.Add(view, pool);
            }

            return (IPool<T>)(object)pool;
        }
    }

    internal sealed class FakeTrafficPool : IPool<TrafficCarView>
    {
        private readonly RoadTestKit _kit;
        private readonly Stack<TrafficCarView> _inactive = new Stack<TrafficCarView>();
        private readonly HashSet<TrafficCarView> _spawned = new HashSet<TrafficCarView>();

        public FakeTrafficPool(TrafficCarView prefab, RoadTestKit kit)
        {
            Prefab = prefab;
            _kit = kit;
        }

        public TrafficCarView Prefab { get; }

        public int CountActive => _spawned.Count;

        public int CountInactive => _inactive.Count;

        public int PrewarmCalls { get; private set; }

        public TrafficCarView Spawn(Transform parent = null)
        {
            return Spawn(Vector3.zero, Quaternion.identity, parent);
        }

        public TrafficCarView Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            TrafficCarView view = _inactive.Count > 0 ? _inactive.Pop() : Create();
            view.transform.SetParent(parent, false);
            view.transform.SetPositionAndRotation(position, rotation);
            view.gameObject.SetActive(true);
            ((IPoolable)view).OnSpawned(default);
            _spawned.Add(view);
            return view;
        }

        public void Despawn(TrafficCarView instance)
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
            foreach (TrafficCarView view in new List<TrafficCarView>(_spawned))
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

        private TrafficCarView Create()
        {
            TrafficCarView view = FakeTrafficPools.MakeView(_kit, $"{Prefab.name}(Clone)");
            view.gameObject.SetActive(false);
            return view;
        }
    }
}
