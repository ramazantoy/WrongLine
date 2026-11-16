using NUnit.Framework;
using UnityEngine;

namespace WrongLine.Road.Tests
{
    public class RoadPieceViewBoundsTests
    {
        private GameObject _object;
        private Mesh _mesh;

        [SetUp]
        public void SetUp()
        {
            _mesh = new Mesh();
            _mesh.vertices = new[] { new Vector3(-1f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 10f) };
            _mesh.triangles = new[] { 0, 1, 2 };
            _mesh.RecalculateBounds();

            _object = new GameObject("piece");
            _object.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _object.AddComponent<MeshRenderer>();
            _object.AddComponent<RoadPieceView>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_object);
            Object.DestroyImmediate(_mesh);
        }

        [Test]
        public void ExpandBounds_GrowsTheRenderersBoundsOnEverySide()
        {
            _object.GetComponent<RoadPieceView>().ExpandBounds(5f);

            Bounds bounds = _object.GetComponent<MeshRenderer>().localBounds;
            Assert.AreEqual(_mesh.bounds.extents + Vector3.one * 5f, bounds.extents);
            Assert.AreEqual(_mesh.bounds.center, bounds.center);
        }

        [Test]
        public void ExpandBounds_CalledAgain_DoesNotGrowThemFurther()
        {
            var view = _object.GetComponent<RoadPieceView>();

            view.ExpandBounds(5f);
            view.ExpandBounds(5f);

            Assert.AreEqual(_mesh.bounds.extents + Vector3.one * 5f, _object.GetComponent<MeshRenderer>().localBounds.extents);
        }

        [Test]
        public void ExpandBounds_NeverTouchesTheSharedMesh()
        {
            Bounds before = _mesh.bounds;

            _object.GetComponent<RoadPieceView>().ExpandBounds(40f);

            Assert.AreEqual(before, _mesh.bounds);
        }

        [Test]
        public void ExpandBounds_NegativeMarginIsTreatedAsNone()
        {
            _object.GetComponent<RoadPieceView>().ExpandBounds(-3f);

            Assert.AreEqual(_mesh.bounds.extents, _object.GetComponent<MeshRenderer>().localBounds.extents);
        }
    }
}
