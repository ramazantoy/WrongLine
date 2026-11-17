using System.Collections.Generic;
using UnityEngine;
using WrongLine.Scenery;

namespace WrongLine.Road.Editor
{
    /// <summary>
    /// Builds the ground strip of a road piece, in the piece's local space: a cross-section every
    /// <see cref="GroundSettings.RingSpacing"/> metres, each running out from the curb's edge on both sides along
    /// <see cref="GroundProfile"/>. The stops across are closer near the road, where the bend and the camera need detail.
    /// The shader maps the texture from above, so the mesh's UV is only a fallback.
    /// </summary>
    public static class GroundMeshBuilder
    {
        /// <summary>Where the vertices across one side sit, as shares of the way from the curb's edge to the ground's reach.</summary>
        internal static readonly float[] Stops = { 0f, 0.04f, 0.12f, 0.28f, 0.55f, 1f };

        private const float UvMeters = 16f;

        public static Mesh Build(TrackShape shape, GroundSettings settings)
        {
            var distances = new List<float>();
            for (float s = 0f; s < shape.Length - 1e-3f; s += settings.RingSpacing)
            {
                distances.Add(s);
            }

            distances.Add(shape.Length);

            int perSide = Stops.Length;
            int perRing = perSide * 2;
            var vertices = new Vector3[distances.Count * perRing];
            var uvs = new Vector2[vertices.Length];
            for (int ring = 0; ring < distances.Count; ring++)
            {
                float s = distances[ring];
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    int side = sideIndex == 0 ? 1 : -1;
                    float reach = GroundProfile.Reach(shape, s, side, settings);
                    for (int stop = 0; stop < perSide; stop++)
                    {
                        float x = side * (GroundProfile.EdgeX + (reach - GroundProfile.EdgeX) * Stops[stop]);
                        int index = ring * perRing + sideIndex * perSide + stop;
                        vertices[index] = GroundProfile.Ground(shape, s, x, reach, settings.EdgeDrop);
                        uvs[index] = new Vector2(x / UvMeters, s / UvMeters);
                    }
                }
            }

            var triangles = new List<int>((distances.Count - 1) * 2 * (perSide - 1) * 6);
            for (int ring = 0; ring < distances.Count - 1; ring++)
            {
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    for (int stop = 0; stop < perSide - 1; stop++)
                    {
                        int a = ring * perRing + sideIndex * perSide + stop;
                        int b = a + perRing;
                        int c = a + 1;
                        int d = b + 1;
                        if (sideIndex == 0)
                        {
                            // Right side: outward is +x. Clockwise seen from above, so the face looks up.
                            triangles.AddRange(new[] { a, b, c, c, b, d });
                        }
                        else
                        {
                            triangles.AddRange(new[] { a, c, b, c, d, b });
                        }
                    }
                }
            }

            var mesh = new Mesh { name = "Ground" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.SetTriangles(triangles, 0);
            mesh.normals = UpwardNormals(vertices, triangles);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Vertex normals from the faces around each vertex, every face counted as facing up. Inside a tight bend the ground
        /// reaches past the bend's centre and folds over itself there; a folded face then lights like the rest of the ground
        /// instead of darkening its neighbours (the material draws both sides and the shader never flips a ground normal).
        /// </summary>
        internal static Vector3[] UpwardNormals(Vector3[] vertices, List<int> triangles)
        {
            var normals = new Vector3[vertices.Length];
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int a = triangles[i];
                int b = triangles[i + 1];
                int c = triangles[i + 2];
                Vector3 face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                if (face.y < 0f)
                {
                    face = -face;
                }

                normals[a] += face;
                normals[b] += face;
                normals[c] += face;
            }

            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = normals[i].sqrMagnitude > 1e-12f ? normals[i].normalized : Vector3.up;
            }

            return normals;
        }
    }
}
