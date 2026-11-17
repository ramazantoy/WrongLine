using System.Collections.Generic;
using UnityEngine;

namespace WrongLine.Scenery.Editor
{
    /// <summary>
    /// Stand-in prop meshes made of a few low-poly primitives, flat shaded, pivot at the middle of the base, +Z towards the
    /// road for the props that face it. Two submeshes: 0 the frame (trunk, pole), 1 the main part (crown, board, lamp).
    /// </summary>
    internal static class PlaceholderMeshes
    {
        public const int FrameSubmesh = 0;
        public const int MainSubmesh = 1;

        public static Mesh Build(PlaceholderShape shape)
        {
            var builder = new Builder();
            switch (shape)
            {
                case PlaceholderShape.TreeCone:
                    builder.Cylinder(Matrix4x4.identity, 0.25f, 0.2f, 1.2f, 6, FrameSubmesh);
                    builder.Cylinder(Matrix4x4.Translate(new Vector3(0f, 1f, 0f)), 1.6f, 0f, 3f, 7, MainSubmesh);
                    builder.Cylinder(Matrix4x4.Translate(new Vector3(0f, 2.8f, 0f)), 1.15f, 0f, 2.6f, 7, MainSubmesh);
                    break;
                case PlaceholderShape.TreeRound:
                    builder.Cylinder(Matrix4x4.identity, 0.28f, 0.2f, 2f, 6, FrameSubmesh);
                    builder.Blob(new Vector3(0f, 3.3f, 0f), new Vector3(1.9f, 1.7f, 1.9f), 4, 7, MainSubmesh);
                    break;
                case PlaceholderShape.TreeBare:
                    builder.Cylinder(Matrix4x4.identity, 0.28f, 0.1f, 4.5f, 6, FrameSubmesh);
                    builder.Box(Matrix4x4.TRS(new Vector3(0.5f, 2.6f, 0f), Quaternion.Euler(0f, 0f, -40f), new Vector3(0.12f, 1.8f, 0.12f)), FrameSubmesh);
                    builder.Box(Matrix4x4.TRS(new Vector3(-0.4f, 3.2f, 0.2f), Quaternion.Euler(20f, 0f, 35f), new Vector3(0.1f, 1.5f, 0.1f)), FrameSubmesh);
                    builder.Box(Matrix4x4.TRS(new Vector3(0f, 3.6f, -0.4f), Quaternion.Euler(-35f, 0f, 0f), new Vector3(0.09f, 1.3f, 0.09f)), FrameSubmesh);
                    break;
                case PlaceholderShape.Bush:
                    builder.Blob(new Vector3(0f, 0.55f, 0f), new Vector3(1.1f, 0.8f, 1.1f), 3, 7, MainSubmesh);
                    break;
                case PlaceholderShape.Rock:
                    builder.Blob(new Vector3(0f, 0.3f, 0f), new Vector3(1.2f, 0.8f, 1f), 3, 5, MainSubmesh);
                    break;
                case PlaceholderShape.Cactus:
                    builder.Cylinder(Matrix4x4.identity, 0.35f, 0.3f, 3f, 7, MainSubmesh);
                    builder.Box(Matrix4x4.TRS(new Vector3(0.5f, 1.3f, 0f), Quaternion.identity, new Vector3(0.6f, 0.3f, 0.3f)), MainSubmesh);
                    builder.Cylinder(Matrix4x4.Translate(new Vector3(0.75f, 1.2f, 0f)), 0.2f, 0.18f, 1.1f, 6, MainSubmesh);
                    builder.Box(Matrix4x4.TRS(new Vector3(-0.45f, 1.9f, 0f), Quaternion.identity, new Vector3(0.5f, 0.26f, 0.26f)), MainSubmesh);
                    builder.Cylinder(Matrix4x4.Translate(new Vector3(-0.65f, 1.8f, 0f)), 0.17f, 0.15f, 0.8f, 6, MainSubmesh);
                    break;
                case PlaceholderShape.StreetLamp:
                    builder.Box(Matrix4x4.TRS(new Vector3(0f, 3f, 0f), Quaternion.identity, new Vector3(0.18f, 6f, 0.18f)), FrameSubmesh);
                    builder.Box(Matrix4x4.TRS(new Vector3(0f, 6f, 0.8f), Quaternion.identity, new Vector3(0.15f, 0.15f, 1.6f)), FrameSubmesh);
                    builder.Box(Matrix4x4.TRS(new Vector3(0f, 5.88f, 1.5f), Quaternion.identity, new Vector3(0.35f, 0.15f, 0.6f)), MainSubmesh);
                    break;
                case PlaceholderShape.Sign:
                    builder.Box(Matrix4x4.TRS(new Vector3(0f, 1.2f, 0f), Quaternion.identity, new Vector3(0.12f, 2.4f, 0.12f)), FrameSubmesh);
                    builder.Box(Matrix4x4.TRS(new Vector3(0f, 2.4f, 0.08f), Quaternion.identity, new Vector3(1.4f, 0.9f, 0.08f)), MainSubmesh);
                    break;
                case PlaceholderShape.UtilityPole:
                    builder.Cylinder(Matrix4x4.identity, 0.16f, 0.12f, 8f, 6, FrameSubmesh);
                    builder.Box(Matrix4x4.TRS(new Vector3(0f, 7.4f, 0f), Quaternion.identity, new Vector3(0.14f, 0.14f, 2.2f)), FrameSubmesh);
                    break;
                case PlaceholderShape.Bale:
                    builder.Cylinder(Matrix4x4.TRS(new Vector3(-0.7f, 0.65f, 0f), Quaternion.Euler(0f, 0f, -90f), Vector3.one), 0.65f, 0.65f, 1.4f, 8, MainSubmesh);
                    break;
                case PlaceholderShape.Fence:
                    builder.Box(Matrix4x4.TRS(new Vector3(-1.8f, 0.55f, 0f), Quaternion.identity, new Vector3(0.12f, 1.1f, 0.12f)), FrameSubmesh);
                    builder.Box(Matrix4x4.TRS(new Vector3(1.8f, 0.55f, 0f), Quaternion.identity, new Vector3(0.12f, 1.1f, 0.12f)), FrameSubmesh);
                    builder.Box(Matrix4x4.TRS(new Vector3(0f, 0.5f, 0f), Quaternion.identity, new Vector3(3.8f, 0.12f, 0.06f)), MainSubmesh);
                    builder.Box(Matrix4x4.TRS(new Vector3(0f, 0.9f, 0f), Quaternion.identity, new Vector3(3.8f, 0.12f, 0.06f)), MainSubmesh);
                    break;
                case PlaceholderShape.Stake:
                    builder.Box(Matrix4x4.TRS(new Vector3(0f, 0.6f, 0f), Quaternion.identity, new Vector3(0.1f, 1.2f, 0.1f)), FrameSubmesh);
                    builder.Box(Matrix4x4.TRS(new Vector3(0f, 1.05f, 0f), Quaternion.identity, new Vector3(0.12f, 0.25f, 0.12f)), MainSubmesh);
                    break;
            }

            return builder.ToMesh(shape.ToString());
        }

        private sealed class Builder
        {
            private readonly List<Vector3>[] _triangles = { new List<Vector3>(), new List<Vector3>() };

            /// <summary>A cylinder (a cone when the top radius is 0) standing on its base at the transform's origin.</summary>
            public void Cylinder(Matrix4x4 transform, float bottomRadius, float topRadius, float height, int sides, int submesh)
            {
                for (int i = 0; i < sides; i++)
                {
                    float a0 = i * Mathf.PI * 2f / sides;
                    float a1 = (i + 1) * Mathf.PI * 2f / sides;
                    Vector3 b0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * bottomRadius;
                    Vector3 b1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * bottomRadius;
                    Vector3 t0 = new Vector3(Mathf.Cos(a0) * topRadius, height, Mathf.Sin(a0) * topRadius);
                    Vector3 t1 = new Vector3(Mathf.Cos(a1) * topRadius, height, Mathf.Sin(a1) * topRadius);
                    Quad(transform, b0, t0, t1, b1, submesh);
                    Triangle(transform, Vector3.zero, b0, b1, submesh); // Bottom, facing down.
                    if (topRadius > 0f)
                    {
                        Triangle(transform, new Vector3(0f, height, 0f), t1, t0, submesh);
                    }
                }
            }

            /// <summary>A unit cube scaled, turned and moved by the transform.</summary>
            public void Box(Matrix4x4 transform, int submesh)
            {
                Vector3 V(float x, float y, float z) => new Vector3(x, y, z) * 0.5f;
                Quad(transform, V(-1, -1, -1), V(-1, 1, -1), V(1, 1, -1), V(1, -1, -1), submesh); // -Z
                Quad(transform, V(1, -1, 1), V(1, 1, 1), V(-1, 1, 1), V(-1, -1, 1), submesh); // +Z
                Quad(transform, V(-1, -1, 1), V(-1, 1, 1), V(-1, 1, -1), V(-1, -1, -1), submesh); // -X
                Quad(transform, V(1, -1, -1), V(1, 1, -1), V(1, 1, 1), V(1, -1, 1), submesh); // +X
                Quad(transform, V(-1, 1, -1), V(-1, 1, 1), V(1, 1, 1), V(1, 1, -1), submesh); // +Y
                Quad(transform, V(-1, -1, 1), V(-1, -1, -1), V(1, -1, -1), V(1, -1, 1), submesh); // -Y
            }

            /// <summary>A low-poly ellipsoid.</summary>
            public void Blob(Vector3 centre, Vector3 radii, int rings, int segments, int submesh)
            {
                Vector3 Point(int ring, int segment)
                {
                    float polar = Mathf.PI * ring / rings;
                    float azimuth = Mathf.PI * 2f * segment / segments + (ring % 2) * Mathf.PI / segments;
                    var unit = new Vector3(Mathf.Sin(polar) * Mathf.Cos(azimuth), Mathf.Cos(polar), Mathf.Sin(polar) * Mathf.Sin(azimuth));
                    return centre + Vector3.Scale(unit, radii);
                }

                for (int ring = 0; ring < rings; ring++)
                {
                    for (int segment = 0; segment < segments; segment++)
                    {
                        Vector3 a = Point(ring, segment);
                        Vector3 b = Point(ring, segment + 1);
                        Vector3 c = Point(ring + 1, segment);
                        Vector3 d = Point(ring + 1, segment + 1);
                        if (ring > 0)
                        {
                            Triangle(Matrix4x4.identity, a, b, c, submesh);
                        }

                        if (ring < rings - 1)
                        {
                            Triangle(Matrix4x4.identity, b, d, c, submesh);
                        }
                    }
                }
            }

            /// <summary>Corners in clockwise order seen from outside.</summary>
            private void Quad(Matrix4x4 transform, Vector3 a, Vector3 b, Vector3 c, Vector3 d, int submesh)
            {
                Triangle(transform, a, b, c, submesh);
                Triangle(transform, a, c, d, submesh);
            }

            private void Triangle(Matrix4x4 transform, Vector3 a, Vector3 b, Vector3 c, int submesh)
            {
                _triangles[submesh].Add(transform.MultiplyPoint3x4(a));
                _triangles[submesh].Add(transform.MultiplyPoint3x4(b));
                _triangles[submesh].Add(transform.MultiplyPoint3x4(c));
            }

            public Mesh ToMesh(string name)
            {
                // Every triangle has its own corners, so the normals come out flat (the low-poly look).
                var vertices = new List<Vector3>();
                var mesh = new Mesh { name = name, subMeshCount = _triangles.Length };
                var indices = new List<int>[_triangles.Length];
                for (int submesh = 0; submesh < _triangles.Length; submesh++)
                {
                    indices[submesh] = new List<int>();
                    foreach (Vector3 corner in _triangles[submesh])
                    {
                        indices[submesh].Add(vertices.Count);
                        vertices.Add(corner);
                    }
                }

                mesh.SetVertices(vertices);
                mesh.SetUVs(0, new Vector2[vertices.Count]);
                for (int submesh = 0; submesh < indices.Length; submesh++)
                {
                    mesh.SetTriangles(indices[submesh], submesh);
                }

                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
