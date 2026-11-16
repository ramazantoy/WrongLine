using System;
using System.Linq;
using AF.Core;
using NUnit.Framework;
using UnityEngine;

namespace WrongLine.Road.Tests
{
    public class RoadPiecePoolTests
    {
        private RoadTestKit _kit;
        private FakeRoadPools _pools;
        private RoadPieceView _start;
        private RoadPieceView[] _prefabs;
        private RoadPiecePool _pool;

        [SetUp]
        public void SetUpPool()
        {
            _kit = new RoadTestKit();
            _pools = new FakeRoadPools(_kit);
            _start = _kit.MakePrefab("Start", TrackRecipe.Straight(60f), true);
            _prefabs = new[]
            {
                _kit.MakePrefab("Straight_M", TrackRecipe.Straight(40f), false),
                _kit.MakePrefab("CurveSoft_R", new TrackRecipe(60f, 35f, YawProfile.Single, 0f, PitchProfile.Bump, 0f), false),
                _kit.MakePrefab("RampUp_S", new TrackRecipe(40f, 0f, YawProfile.Single, 8f, PitchProfile.Bump, 0f), false),
            };
            _pool = new RoadPiecePool(_pools, _kit.MakeCatalog(_start, _prefabs), _kit.MakeConfig());
        }

        [TearDown]
        public void TearDownPool()
        {
            _kit.Dispose();
        }

        [Test]
        public void TheSet_NumbersTheStartPieceZero_AndTheOthersInCatalogOrder()
        {
            Assert.AreEqual(4, _pool.Set.Infos.Count);
            Assert.IsTrue(_pool.Set.Start.IsStart);
            Assert.AreEqual(new[] { "Start", "Straight_M", "CurveSoft_R", "RampUp_S" }, _pool.Set.Infos.Select(info => info.Name).ToArray());
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, _pool.Set.Infos.Select(info => info.Id).ToArray());
            Assert.AreSame(_prefabs[1], _pool.Set.Prefabs[2]);
        }

        [Test]
        public void Constructing_MakesNoInstances_PrewarmingMakesThem()
        {
            Assert.AreEqual(0, _pools.Pools.Values.Sum(pool => pool.CreatedCount));

            _pool.Prewarm();

            Assert.AreEqual(1, _pools.Pools[_start].CountInactive, "One start piece is enough.");
            foreach (RoadPieceView prefab in _prefabs)
            {
                Assert.AreEqual(3, _pools.Pools[prefab].CountInactive, prefab.name);
            }
        }

        [Test]
        public void PrewarmingTwice_DoesNothingTheSecondTime()
        {
            _pool.Prewarm();
            _pool.Prewarm();

            Assert.IsTrue(_pools.Pools.Values.All(pool => pool.PrewarmCalls == 1));
        }

        [Test]
        public void Spawn_GivesAnActivePieceAtThePlace_BoundToItsIndex_TakenFromTheWaitingOnes()
        {
            _pool.Prewarm();
            var position = new Vector3(10f, 2f, 30f);
            Quaternion rotation = Quaternion.Euler(0f, 40f, 0f);

            RoadPieceView view = _pool.Spawn(17, _pool.Set.Infos[2], position, rotation);

            Assert.IsTrue(view.gameObject.activeSelf);
            Assert.AreEqual(position, view.transform.position);
            Assert.AreEqual(0f, Quaternion.Angle(rotation, view.transform.rotation), 1e-3f);
            Assert.AreEqual(17, view.PlacedIndex);
            Assert.AreEqual(1, _pool.CountActive);
            Assert.AreEqual(2, _pools.Pools[_prefabs[1]].CountInactive, "A waiting instance was used, none was made.");
            Assert.AreEqual(3, _pools.Pools[_prefabs[1]].CreatedCount);
        }

        [Test]
        public void Release_SwitchesThePieceOffAndResetsIt_WithoutTheCallerDoingAnything()
        {
            _pool.Prewarm();
            RoadPieceView view = _pool.Spawn(5, _pool.Set.Infos[1], new Vector3(4f, 5f, 6f), Quaternion.Euler(0f, 90f, 0f));

            _pool.Release(view);

            Assert.IsFalse(view.gameObject.activeSelf, "Switched off.");
            Assert.AreEqual(RoadPieceView.NotPlaced, view.PlacedIndex, "No index kept.");
            Assert.AreEqual(Vector3.zero, view.transform.position, "No position kept.");
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, view.transform.rotation), 1e-3f, "No rotation kept.");
            Assert.AreEqual(0, _pool.CountActive);
            Assert.AreEqual(3, _pools.Pools[_prefabs[0]].CountInactive);
        }

        [Test]
        public void AReleasedPiece_IsTheOneSpawnedNextAndComesBackFresh()
        {
            _pool.Prewarm();
            RoadPieceView first = _pool.Spawn(1, _pool.Set.Infos[1], Vector3.zero, Quaternion.identity);
            _pool.Release(first);

            RoadPieceView second = _pool.Spawn(99, _pool.Set.Infos[1], new Vector3(1f, 0f, 0f), Quaternion.identity);

            Assert.AreSame(first, second);
            Assert.AreEqual(99, second.PlacedIndex);
            Assert.IsTrue(second.gameObject.activeSelf);
        }

        [Test]
        public void ReleasingTwice_ReleasingAStranger_OrNull_IsRejected()
        {
            _pool.Prewarm();
            RoadPieceView view = _pool.Spawn(1, _pool.Set.Infos[1], Vector3.zero, Quaternion.identity);
            _pool.Release(view);

            Assert.Throws<ArgumentException>(() => _pool.Release(view));
            Assert.Throws<ArgumentException>(() => _pool.Release(_prefabs[0]));
            Assert.Throws<ArgumentException>(() => _pool.Release(null));
        }

        [Test]
        public void ReleaseAll_TakesEverythingBack()
        {
            _pool.Prewarm();
            for (int i = 0; i < 6; i++)
            {
                _pool.Spawn(i, _pool.Set.Infos[1 + i % 3], new Vector3(i, 0f, 0f), Quaternion.identity);
            }

            _pool.ReleaseAll();

            Assert.AreEqual(0, _pool.CountActive);
            Assert.IsTrue(_pools.Pools.Values.All(pool => pool.CountActive == 0));
        }

        [Test]
        public void ChurningThroughTheRoad_MakesNoNewInstances()
        {
            _pool.Prewarm();
            int made = _pools.Pools.Values.Sum(pool => pool.CreatedCount);
            var live = new System.Collections.Generic.Queue<RoadPieceView>();

            for (int i = 0; i < 2000; i++)
            {
                live.Enqueue(_pool.Spawn(i, _pool.Set.Infos[1 + i % 3], new Vector3(i, 0f, 0f), Quaternion.identity));
                if (live.Count > 3)
                {
                    _pool.Release(live.Dequeue());
                }
            }

            Assert.AreEqual(made, _pools.Pools.Values.Sum(pool => pool.CreatedCount), "A window of three pieces never needs more than were prewarmed.");
            Assert.AreEqual(3, _pool.CountActive);
        }

        [Test]
        public void ASpawnWithoutPrewarming_StillWorks_ByMakingTheInstanceThen()
        {
            RoadPieceView view = _pool.Spawn(0, _pool.Set.Start, Vector3.zero, Quaternion.identity);

            Assert.IsTrue(view.gameObject.activeSelf);
            Assert.AreEqual(1, _pools.Pools[_start].CreatedCount);
        }

        [Test]
        public void AnUnknownPiece_IsRejected()
        {
            var stranger = new RoadPieceInfo(42, "Stranger", TrackRecipe.Straight(20f), false);

            Assert.Throws<ArgumentOutOfRangeException>(() => _pool.Spawn(0, stranger, Vector3.zero, Quaternion.identity));
        }

        [Test]
        public void ACatalogWithoutAStart_OrWithAnEmptySlot_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new RoadPiecePool(_pools, _kit.MakeCatalog(null, _prefabs), _kit.MakeConfig()));
            Assert.Throws<ArgumentException>(() => new RoadPiecePool(_pools, _kit.MakeCatalog(_start, _prefabs[0], null), _kit.MakeConfig()));
        }

        [Test]
        public void ANonsenseConfig_IsRejectedWhenThePoolIsMade()
        {
            RoadConfig bad = _kit.MakeConfig(serialized => serialized.FindProperty("_aheadMeters").floatValue = -1f);

            Assert.Throws<ArgumentException>(() => new RoadPiecePool(_pools, _kit.MakeCatalog(_start, _prefabs), bad));
        }

        [Test]
        public void ThePieceView_ResetsItself_WhenDespawned()
        {
            RoadPieceView view = _prefabs[0];
            view.Bind(7);
            view.transform.SetPositionAndRotation(new Vector3(3f, 3f, 3f), Quaternion.Euler(0f, 10f, 0f));

            ((IPoolable)view).OnDespawned();

            Assert.AreEqual(RoadPieceView.NotPlaced, view.PlacedIndex);
            Assert.AreEqual(Vector3.zero, view.transform.position);
        }
    }
}
