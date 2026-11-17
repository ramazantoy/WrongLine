using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WrongLine.Road;
using WrongLine.Road.Editor;
using WrongLine.Road.Tests;

namespace WrongLine.Scenery.Tests
{
    public class GroundMeshBuilderTests
    {
        private static readonly GroundSettings _settings = new GroundSettings(0.3f, 80f, 0.8f, 20f, 2f);

        private static readonly GroundSettings _defaults = new GroundSettings();

        [Test]
        public void OnAStraight_EveryTriangleFacesUp()
        {
            foreach (RoadPieceInfo piece in RoadTestPieces.All())
            {
                if ((piece.Tags & ~RoadTags.Straight) != RoadTags.None)
                {
                    continue;
                }

                Mesh mesh = GroundMeshBuilder.Build(piece.Shape, _defaults);
                try
                {
                    Vector3[] vertices = mesh.vertices;
                    int[] triangles = mesh.triangles;
                    Assert.Greater(triangles.Length, 0, piece.Name);
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        Vector3 normal = Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]], vertices[triangles[i + 2]] - vertices[triangles[i]]);
                        Assert.Greater(normal.y, 0f, $"{piece.Name}: triangle {i / 3} faces down.");
                    }
                }
                finally
                {
                    Object.DestroyImmediate(mesh);
                }
            }
        }

        [Test]
        public void OnEveryDefaultPiece_EveryNormalPointsUp_EvenWhereTheGroundFolds()
        {
            foreach (RoadPieceInfo piece in RoadTestPieces.All())
            {
                Mesh mesh = GroundMeshBuilder.Build(piece.Shape, _defaults);
                try
                {
                    foreach (Vector3 normal in mesh.normals)
                    {
                        Assert.Greater(normal.y, 0.2f, $"{piece.Name}: a ground normal points sideways or down, it would light darker.");
                    }
                }
                finally
                {
                    Object.DestroyImmediate(mesh);
                }
            }
        }

        [Test]
        public void InsideEveryTightBend_TheGroundCoversTheBendsCentre_NoHoleInTheCorner()
        {
            int bends = 0;
            foreach (RoadPieceInfo piece in RoadTestPieces.All())
            {
                if (Mathf.Abs((float)piece.ExitYaw) * Mathf.Rad2Deg < 45f)
                {
                    continue;
                }

                bends++;
                float middle = piece.Length * 0.5f;
                float curvature = GroundProfile.Curvature(piece.Shape, middle);
                TrackFrame frame = piece.Shape.Evaluate(middle);
                Vector3 centre = frame.Position + GroundProfile.FlatRight(frame) / curvature; // Its sign picks the inner side.
                Mesh mesh = GroundMeshBuilder.Build(piece.Shape, _defaults);
                try
                {
                    Assert.IsTrue(Covers(mesh, centre), $"{piece.Name}: the inside of the bend has no ground at its centre {centre}.");
                }
                finally
                {
                    Object.DestroyImmediate(mesh);
                }
            }

            Assert.GreaterOrEqual(bends, 4, "CurveSharp and BankSharp, both ways.");
        }

        /// <summary>Whether any triangle of the mesh lies over the point, seen from above.</summary>
        private static bool Covers(Mesh mesh, Vector3 point)
        {
            Vector3[] v = mesh.vertices;
            int[] t = mesh.triangles;
            var p = new Vector2(point.x, point.z);
            for (int i = 0; i < t.Length; i += 3)
            {
                var a = new Vector2(v[t[i]].x, v[t[i]].z);
                var b = new Vector2(v[t[i + 1]].x, v[t[i + 1]].z);
                var c = new Vector2(v[t[i + 2]].x, v[t[i + 2]].z);
                float d1 = Cross(b - a, p - a);
                float d2 = Cross(c - b, p - b);
                float d3 = Cross(a - c, p - c);
                bool negative = d1 < 0f || d2 < 0f || d3 < 0f;
                bool positive = d1 > 0f || d2 > 0f || d3 > 0f;
                if (!(negative && positive))
                {
                    return true;
                }
            }

            return false;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        [Test]
        public void TheGround_MeetsTheCurbsEdge_AtBothEndsOfEveryPiece_SoNeighboursJoin()
        {
            foreach (RoadPieceInfo piece in RoadTestPieces.All())
            {
                Mesh mesh = GroundMeshBuilder.Build(piece.Shape, _settings);
                try
                {
                    var edges = new List<Vector3>();
                    Vector3[] vertices = mesh.vertices;
                    int perSide = GroundMeshBuilder.Stops.Length;
                    int rings = vertices.Length / (perSide * 2);
                    foreach (int ring in new[] { 0, rings - 1 })
                    {
                        TrackFrame frame = ring == 0 ? piece.Shape.Entry : piece.Shape.Exit;
                        Vector3 right = vertices[ring * perSide * 2];
                        Vector3 left = vertices[ring * perSide * 2 + perSide];
                        Assert.Less(Vector3.Distance(right, frame.Position + frame.Right * GroundProfile.EdgeX - frame.Up * 0.3f), 1e-3f, piece.Name);
                        Assert.Less(Vector3.Distance(left, frame.Position - frame.Right * GroundProfile.EdgeX - frame.Up * 0.3f), 1e-3f, piece.Name);
                    }
                }
                finally
                {
                    Object.DestroyImmediate(mesh);
                }
            }
        }

        [Test]
        public void AStraight_ReachesTheFullWidth_WithACrossSectionEveryRingSpacing()
        {
            Mesh mesh = GroundMeshBuilder.Build(TrackIntegrator.Bake(TrackRecipe.Straight(40f)), _settings);
            try
            {
                Bounds bounds = mesh.bounds;
                Assert.AreEqual(GroundProfile.EdgeX + 80f, bounds.max.x, 1e-3f);
                Assert.AreEqual(-(GroundProfile.EdgeX + 80f), bounds.min.x, 1e-3f);
                Assert.AreEqual(-0.3f, bounds.max.y, 1e-4f, "Level, below the asphalt.");
                Assert.AreEqual(21 * GroundMeshBuilder.Stops.Length * 2, mesh.vertexCount, "40 m every 2 m: 21 cross-sections.");
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }
    }
}
