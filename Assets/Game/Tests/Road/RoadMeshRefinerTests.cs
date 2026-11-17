using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using WrongLine.Road.Editor;

namespace WrongLine.Road.Tests
{
    public class RoadMeshRefinerTests
    {
        private readonly List<Mesh> _made = new List<Mesh>();

        [TearDown]
        public void TearDown()
        {
            foreach (Mesh mesh in _made)
            {
                if (mesh != null)
                {
                    Object.DestroyImmediate(mesh);
                }
            }

            _made.Clear();
        }

        private Mesh Track(Mesh mesh)
        {
            _made.Add(mesh);
            return mesh;
        }

        /// <summary>A flat strip, 6 m wide (x 0, 3, 6) and <paramref name="length"/> long, in two submeshes (the left and the right column).</summary>
        private Mesh Strip(float length)
        {
            Mesh mesh = Track(new Mesh());
            mesh.vertices = new[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(3f, 0f, 0f), new Vector3(6f, 0f, 0f),
                new Vector3(0f, 0f, length), new Vector3(3f, 0f, length), new Vector3(6f, 0f, length),
            };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(0f, length / 9f), new Vector2(0.5f, length / 9f), new Vector2(1f, length / 9f) };
            mesh.normals = Enumerable.Repeat(Vector3.up, 6).ToArray();
            mesh.subMeshCount = 2;
            mesh.SetTriangles(new[] { 0, 3, 4, 0, 4, 1 }, 0);
            mesh.SetTriangles(new[] { 1, 4, 5, 1, 5, 2 }, 1);
            return mesh;
        }

        private static float Area(Mesh mesh, int submesh)
        {
            Vector3[] v = mesh.vertices;
            int[] t = mesh.GetTriangles(submesh);
            float area = 0f;
            for (int i = 0; i < t.Length; i += 3)
            {
                area += Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).magnitude * 0.5f;
            }

            return area;
        }

        [Test]
        public void NoSliceIsLongerThanTheStepAfterwards_AndItWasCutIntoEqualOnes()
        {
            Mesh refined = Track(RoadMeshRefiner.SliceAlongRoad(Strip(120f), 8f));

            Assert.AreEqual(8f, RoadMeshRefiner.LongestRingGap(refined), 1e-3f, "120 m in 15 slices of 8 m");
            Assert.AreEqual(15, refined.vertices.Select(v => Mathf.RoundToInt(v.z * 1000f)).Distinct().Count() - 1);

            Mesh odd = Track(RoadMeshRefiner.SliceAlongRoad(Strip(100f), 8f));
            Assert.LessOrEqual(RoadMeshRefiner.LongestRingGap(odd), 8f + 1e-3f);
        }

        [Test]
        public void ShapeAreaNormalsAndSubmeshesAreKept_AndTheUvRunsOnLinearly()
        {
            Mesh source = Strip(120f);
            Mesh refined = Track(RoadMeshRefiner.SliceAlongRoad(source, 8f));

            Assert.AreEqual(2, refined.subMeshCount);
            Assert.AreEqual(Area(source, 0), Area(refined, 0), 1e-2f);
            Assert.AreEqual(Area(source, 1), Area(refined, 1), 1e-2f);
            Assert.IsTrue(refined.normals.All(n => Vector3.Dot(n, Vector3.up) > 0.999f));
            Assert.IsTrue(Enumerable.Range(0, 2).All(s => refined.GetTriangles(s).Where((_, i) => i % 3 == 0).Any()), "no submesh lost its triangles");
            Vector3[] v = refined.vertices;
            Vector2[] uv = refined.uv;
            for (int i = 0; i < v.Length; i++)
            {
                Assert.AreEqual(v[i].z / 9f, uv[i].y, 1e-4f, "v follows the distance along the strip");
                Assert.AreEqual(v[i].x / 6f, uv[i].x, 1e-4f, "u follows the width");
            }

            // Every triangle still faces up (the winding is kept).
            foreach (int submesh in new[] { 0, 1 })
            {
                int[] t = refined.GetTriangles(submesh);
                for (int i = 0; i < t.Length; i += 3)
                {
                    Vector3 normal = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                    Assert.Greater(normal.normalized.y, 0.999f, "winding kept");
                }
            }
        }

        [Test]
        public void NeighboursAgreeOnEveryEdge_SoNoGapOpens()
        {
            Mesh refined = Track(RoadMeshRefiner.SliceAlongRoad(Strip(120f), 8f));
            Vector3[] v = refined.vertices;

            // The middle line (x = 3) is shared by the two submeshes: both sides must be cut at the same places.
            CollectionAssert.AreEqual(OnLine(refined, 0, v), OnLine(refined, 1, v), "the two sides of the shared edge are cut at the same places");

            // Inside the strip each edge is used by two triangles; an edge used once is on the outside (a T-junction would show up here).
            var uses = new Dictionary<(int, int), int>();
            for (int s = 0; s < 2; s++)
            {
                int[] t = refined.GetTriangles(s);
                for (int i = 0; i < t.Length; i += 3)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        int a = t[i + k];
                        int b = t[i + (k + 1) % 3];
                        (int, int) key = a <= b ? (a, b) : (b, a);
                        uses[key] = uses.TryGetValue(key, out int n) ? n + 1 : 1;
                    }
                }
            }

            foreach (KeyValuePair<(int, int), int> pair in uses)
            {
                if (pair.Value == 1)
                {
                    Vector3 a = v[pair.Key.Item1];
                    Vector3 b = v[pair.Key.Item2];
                    bool outside = (Mathf.Approximately(a.x, 0f) && Mathf.Approximately(b.x, 0f)) || (Mathf.Approximately(a.x, 6f) && Mathf.Approximately(b.x, 6f))
                        || (Mathf.Approximately(a.z, 0f) && Mathf.Approximately(b.z, 0f)) || (Mathf.Approximately(a.z, 120f) && Mathf.Approximately(b.z, 120f));
                    Assert.IsTrue(outside, $"an edge in the middle of the strip is used once: {a} to {b}");
                }
            }
        }

        /// <summary>The distinct z (in mm) of the vertices on the line x = 3 that the submesh uses, ascending.</summary>
        private static List<int> OnLine(Mesh mesh, int submesh, Vector3[] v)
        {
            return mesh.GetTriangles(submesh).Where(i => Mathf.Approximately(v[i].x, 3f)).Select(i => Mathf.RoundToInt(v[i].z * 1000f)).Distinct().OrderBy(z => z).ToList();
        }

        [Test]
        public void AMeshThatIsNotAnExtrusion_OrIsShortAlready_IsReturnedAsItIs()
        {
            Mesh bent = Strip(120f);
            bent.vertices = bent.vertices.Select((v, i) => i == 4 ? new Vector3(v.x, v.y, 60f) : v).ToArray(); // A vertex between the rings.
            Assert.AreSame(bent, RoadMeshRefiner.SliceAlongRoad(bent, 8f));

            Mesh shortStrip = Strip(6f);
            Assert.AreSame(shortStrip, RoadMeshRefiner.SliceAlongRoad(shortStrip, 8f));
        }

        [Test]
        public void TheSagBetweenRingsStaysSmall_UnderTheRealBend()
        {
            // The straight 120 m piece: a ring every 120 m sags 1.4 m under the bend (k L^2 / 4); every 8 m, under a centimetre.
            const float Strength = 0.0004f;
            Mesh refined = Track(RoadMeshRefiner.SliceAlongRoad(Strip(120f), 8f));
            float gap = RoadMeshRefiner.LongestRingGap(refined);

            Assert.Less(Strength * gap * gap / 4f, 0.01f);
            Assert.AreEqual(1.44f, Strength * 120f * 120f / 4f, 1e-3f, "and unrefined it would be this");
        }
    }
}
