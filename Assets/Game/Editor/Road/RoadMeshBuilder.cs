using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WrongLine.Road.Editor
{
    /// <summary>
    /// Sweeps the shared road cross-section (<see cref="RoadSection"/>) along a baked <see cref="TrackShape"/> into a mesh.
    /// Four submeshes, in this order: asphalt, curbs, lane lines, body (underside and outer walls). Faces have hard edges
    /// (every strip has its own vertices) and the winding matches the normals.
    /// </summary>
    public static class RoadMeshBuilder
    {
        public const int SubmeshAsphalt = 0;
        public const int SubmeshCurb = 1;
        public const int SubmeshLine = 2;
        public const int SubmeshBody = 3;
        public const int SubmeshCount = 4;

        private const float OuterX = RoadSection.HalfWidth + RoadSection.CurbWidth;

        // Each segment is traversed so that its normal, the direction rotated 90 degrees counter-clockwise in the (x, y)
        // plane, faces out of the solid.
        private static readonly Segment[][] _profile =
        {
            new[] { new Segment(-RoadSection.HalfWidth, 0f, RoadSection.HalfWidth, 0f) },
            new[]
            {
                new Segment(RoadSection.HalfWidth, 0f, RoadSection.HalfWidth, RoadSection.CurbHeight),
                new Segment(RoadSection.HalfWidth, RoadSection.CurbHeight, OuterX, RoadSection.CurbHeight),
                new Segment(-OuterX, RoadSection.CurbHeight, -RoadSection.HalfWidth, RoadSection.CurbHeight),
                new Segment(-RoadSection.HalfWidth, RoadSection.CurbHeight, -RoadSection.HalfWidth, 0f),
            },
            LineSegments(),
            new[]
            {
                new Segment(OuterX, RoadSection.CurbHeight, OuterX, -RoadSection.Thickness),
                new Segment(OuterX, -RoadSection.Thickness, -OuterX, -RoadSection.Thickness),
                new Segment(-OuterX, -RoadSection.Thickness, -OuterX, RoadSection.CurbHeight),
            },
        };

        private static Segment[] LineSegments()
        {
            var segments = new Segment[RoadSection.LineCount];
            for (int i = 0; i < segments.Length; i++)
            {
                float x = RoadSection.LineX(i);
                segments[i] = new Segment(x - RoadSection.LineWidth * 0.5f, RoadSection.LineHeight, x + RoadSection.LineWidth * 0.5f, RoadSection.LineHeight);
            }

            return segments;
        }

        /// <param name="shape">The baked centre line.</param>
        /// <param name="ringStride">Frames between two cross-sections of the mesh; 1 uses every frame.</param>
        public static Mesh Build(TrackShape shape, int ringStride)
        {
            List<int> rings = RingIndices(shape.Count, ringStride);
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>[SubmeshCount];

            for (int submesh = 0; submesh < SubmeshCount; submesh++)
            {
                triangles[submesh] = new List<int>();
                foreach (Segment segment in _profile[submesh])
                {
                    AddStrip(shape, rings, segment, vertices, normals, uvs, triangles[submesh]);
                }
            }

            var mesh = new Mesh();
            if (vertices.Count > 65535)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = SubmeshCount;
            for (int submesh = 0; submesh < SubmeshCount; submesh++)
            {
                mesh.SetTriangles(triangles[submesh], submesh);
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>How far the first asphalt ring's centre is from the shape's entry frame (the asphalt is the first strip built).</summary>
        public static float EntryGap(Mesh mesh, TrackShape shape)
        {
            var vertices = new List<Vector3>();
            mesh.GetVertices(vertices);
            return Vector3.Distance((vertices[0] + vertices[1]) * 0.5f, shape.Entry.Position);
        }

        /// <summary>How far the last asphalt ring's centre is from the shape's exit frame.</summary>
        public static float ExitGap(Mesh mesh, TrackShape shape)
        {
            var vertices = new List<Vector3>();
            mesh.GetVertices(vertices);
            int[] asphalt = mesh.GetIndices(SubmeshAsphalt);

            // The last quad's triangles end (a0, b0, b1, a0, b1, a1): b0 and b1 are the last ring's left and right edges.
            Vector3 left = vertices[asphalt[asphalt.Length - 5]];
            Vector3 right = vertices[asphalt[asphalt.Length - 4]];
            return Vector3.Distance((left + right) * 0.5f, shape.Exit.Position);
        }

        /// <summary>The frames the mesh gets a cross-section at: every <paramref name="stride"/>th, and always the last.</summary>
        public static List<int> RingIndices(int frameCount, int stride)
        {
            if (stride < 1)
            {
                throw new System.ArgumentOutOfRangeException(nameof(stride));
            }

            var rings = new List<int>();
            for (int i = 0; i < frameCount - 1; i += stride)
            {
                rings.Add(i);
            }

            rings.Add(frameCount - 1);
            return rings;
        }

        private static void AddStrip(TrackShape shape, List<int> rings, Segment segment, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles)
        {
            int first = vertices.Count;
            Vector2 direction = (segment.To - segment.From).normalized;
            var normal2D = new Vector2(-direction.y, direction.x);
            float uFrom = (segment.From.x + RoadSection.HalfWidth) / (2f * RoadSection.HalfWidth);
            float uTo = (segment.To.x + RoadSection.HalfWidth) / (2f * RoadSection.HalfWidth);

            for (int r = 0; r < rings.Count; r++)
            {
                TrackFrame frame = shape[rings[r]];
                float v = rings[r] * shape.Step / RoadSection.TileLength;
                Vector3 normal = frame.Rotation * new Vector3(normal2D.x, normal2D.y, 0f);

                vertices.Add(frame.Position + frame.Rotation * new Vector3(segment.From.x, segment.From.y, 0f));
                vertices.Add(frame.Position + frame.Rotation * new Vector3(segment.To.x, segment.To.y, 0f));
                normals.Add(normal);
                normals.Add(normal);
                uvs.Add(new Vector2(uFrom, v));
                uvs.Add(new Vector2(uTo, v));
            }

            for (int r = 0; r < rings.Count - 1; r++)
            {
                int a0 = first + 2 * r;
                int a1 = a0 + 1;
                int b0 = a0 + 2;
                int b1 = a0 + 3;
                triangles.Add(a0);
                triangles.Add(b0);
                triangles.Add(b1);
                triangles.Add(a0);
                triangles.Add(b1);
                triangles.Add(a1);
            }
        }

        private readonly struct Segment
        {
            public readonly Vector2 From;
            public readonly Vector2 To;

            public Segment(float fromX, float fromY, float toX, float toY)
            {
                From = new Vector2(fromX, fromY);
                To = new Vector2(toX, toY);
            }
        }
    }
}
