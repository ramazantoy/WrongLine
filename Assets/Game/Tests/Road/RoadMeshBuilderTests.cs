using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WrongLine.Road.Editor;

namespace WrongLine.Road.Tests
{
    public class RoadMeshBuilderTests
    {
        private const float Tolerance = 1e-3f;

        [Test]
        public void Straight_SpansTheLength_AndTheSectionWidth()
        {
            TrackShape shape = TrackIntegrator.Bake(TrackRecipe.Straight(20f));

            Mesh mesh = RoadMeshBuilder.Build(shape, shape.Count - 1);

            Assert.AreEqual(0f, mesh.bounds.min.z, Tolerance);
            Assert.AreEqual(20f, mesh.bounds.max.z, Tolerance);
            Assert.AreEqual(RoadSection.HalfWidth + RoadSection.CurbWidth, mesh.bounds.max.x, Tolerance);
            Assert.AreEqual(-RoadSection.Thickness, mesh.bounds.min.y, Tolerance);
            Assert.AreEqual(RoadSection.CurbHeight, mesh.bounds.max.y, Tolerance);
            Assert.AreEqual(RoadMeshBuilder.SubmeshCount, mesh.subMeshCount);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void FirstAndLastAsphaltRing_SitOnTheEntryAndExitFrames()
        {
            TrackShape shape = TrackIntegrator.Bake(new TrackRecipe(60f, 70f, YawProfile.Single, 10f, PitchProfile.Bump, 15f));
            Mesh mesh = RoadMeshBuilder.Build(shape, 2);
            var vertices = new List<Vector3>();
            mesh.GetVertices(vertices);

            // The asphalt is the first strip: its vertices come in (left, right) pairs, one pair per ring.
            AssertRing(shape.Entry, vertices[0], vertices[1]);
            int last = (mesh.GetIndices(RoadMeshBuilder.SubmeshAsphalt).Length / 6) * 2;
            AssertRing(shape.Exit, vertices[last], vertices[last + 1]);
            Object.DestroyImmediate(mesh);
        }

        [TestCase(0f, 0f, 0f)]
        [TestCase(80f, 0f, 20f)]
        [TestCase(-60f, 12f, -15f)]
        public void WindingAgreesWithTheNormals_ForEveryTriangle(float yaw, float pitch, float bank)
        {
            TrackShape shape = TrackIntegrator.Bake(new TrackRecipe(40f, yaw, YawProfile.Single, pitch, PitchProfile.Bump, bank));
            Mesh mesh = RoadMeshBuilder.Build(shape, 2);
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            mesh.GetVertices(vertices);
            mesh.GetNormals(normals);

            int checkedTriangles = 0;
            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                int[] indices = mesh.GetIndices(submesh);
                for (int i = 0; i < indices.Length; i += 3)
                {
                    Vector3 a = vertices[indices[i]];
                    Vector3 b = vertices[indices[i + 1]];
                    Vector3 c = vertices[indices[i + 2]];
                    Vector3 facing = Vector3.Cross(b - a, c - a); // Unity's front face is clockwise: this points at the viewer.
                    // Not 1: on a bend the face's own normal and the strip's normal differ by a few degrees. A flipped triangle gives -0.99.
                    Assert.Greater(Vector3.Dot(facing.normalized, normals[indices[i]]), 0.9f, $"Submesh {submesh}, triangle {i / 3}.");
                    checkedTriangles++;
                }
            }

            Assert.Greater(checkedTriangles, 100);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void Asphalt_FacesUp_AndFollowsTheBank()
        {
            TrackShape shape = TrackIntegrator.Bake(new TrackRecipe(40f, 60f, YawProfile.Single, 0f, PitchProfile.Bump, 20f));
            Mesh mesh = RoadMeshBuilder.Build(shape, 2);
            var normals = new List<Vector3>();
            mesh.GetNormals(normals);
            int[] asphalt = mesh.GetIndices(RoadMeshBuilder.SubmeshAsphalt);

            float tilt = 0f;
            foreach (int index in asphalt)
            {
                Assert.Greater(normals[index].y, 0.9f, "The asphalt faces up.");
                tilt = Mathf.Max(tilt, Vector3.Angle(Vector3.up, normals[index]));
            }

            Assert.AreEqual(20f, tilt, 0.1f, "The steepest tilt of the surface is the bank angle.");
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void Uv_RunsZeroToOneAcrossTheAsphalt_AndByTileLengthAlong()
        {
            TrackShape shape = TrackIntegrator.Bake(TrackRecipe.Straight(18f));
            Mesh mesh = RoadMeshBuilder.Build(shape, shape.Count - 1);
            var uvs = new List<Vector2>();
            mesh.GetUVs(0, uvs);

            Assert.AreEqual(new Vector2(0f, 0f), uvs[0]);
            Assert.AreEqual(new Vector2(1f, 0f), uvs[1]);
            Assert.AreEqual(18f / RoadSection.TileLength, uvs[2].y, Tolerance);
            Assert.AreEqual(2f, uvs[2].y, Tolerance);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void RingIndices_StepByTheStride_AndAlwaysEndOnTheLastFrame()
        {
            CollectionAssert.AreEqual(new[] { 0, 2, 4, 6 }, RoadMeshBuilder.RingIndices(7, 2));
            CollectionAssert.AreEqual(new[] { 0, 2, 4, 5 }, RoadMeshBuilder.RingIndices(6, 2));
            CollectionAssert.AreEqual(new[] { 0, 4 }, RoadMeshBuilder.RingIndices(5, 4));
        }

        private static void AssertRing(TrackFrame frame, Vector3 left, Vector3 right)
        {
            Vector3 expectedLeft = frame.Position - frame.Right * RoadSection.HalfWidth;
            Vector3 expectedRight = frame.Position + frame.Right * RoadSection.HalfWidth;
            Assert.AreEqual(0f, Vector3.Distance(expectedLeft, left), Tolerance);
            Assert.AreEqual(0f, Vector3.Distance(expectedRight, right), Tolerance);
        }
    }
}
