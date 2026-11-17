using System;
using System.Collections.Generic;
using UnityEngine;

namespace WrongLine.Road.Editor
{
    /// <summary>
    /// Cuts the mesh of a flat straight road piece into slices along the road. The curve shader bends every vertex by its own
    /// distance from the camera, and between vertices the surface is straight: a flat straight has only two rings (20 to 120 m
    /// apart), so its road in the middle sits below where the bend puts a car or a prop, which follow the true curve (measured:
    /// 4 cm for a 20 m piece up to 1,4 m for a 120 m one). Every other piece has a ring every few metres and needs nothing.
    /// <para>
    /// A flat straight is an extrusion: every vertex is on one of two rings, at the piece's start and end. Each triangle
    /// stretches from one ring to the other, so it is cut by equally spaced planes across the road into a strip of quads (the last
    /// piece a triangle); the cut points are shared per edge, so neighbours always agree and no gap opens. Positions, normals, UVs,
    /// tangents and colours are interpolated along the cut edges, submeshes kept. A mesh that is not such an extrusion, or whose
    /// slices would not be shorter than the step, is returned as it is.
    /// </para>
    /// </summary>
    public static class RoadMeshRefiner
    {
        private const float RingTolerance = 1e-3f;

        /// <param name="maxStep">The longest slice along the road that may be left, in metres.</param>
        public static Mesh SliceAlongRoad(Mesh source, float maxStep)
        {
            if (!(maxStep > 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(maxStep));
            }

            var vertices = new List<Vector3>(source.vertices);
            if (vertices.Count == 0 || !TryRings(vertices, out float start, out float end))
            {
                return source;
            }

            int slices = Mathf.CeilToInt((end - start) / maxStep - 1e-4f);
            if (slices <= 1)
            {
                return source;
            }

            var normals = new List<Vector3>(source.normals);
            var tangents = new List<Vector4>(source.tangents);
            var colors = new List<Color>(source.colors);
            var uv0 = new List<Vector2>();
            var uv1 = new List<Vector2>();
            source.GetUVs(0, uv0);
            source.GetUVs(1, uv1);

            var cuts = new Dictionary<(int, int, int), int>();
            var submeshes = new List<List<int>>();
            for (int s = 0; s < source.subMeshCount; s++)
            {
                int[] triangles = source.GetTriangles(s);
                var cut = new List<int>(triangles.Length * slices);
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    SliceTriangle(cut, triangles[t], triangles[t + 1], triangles[t + 2], slices, vertices, normals, tangents, colors, uv0, uv1, cuts);
                }

                submeshes.Add(cut);
            }

            var result = new Mesh { name = source.name, indexFormat = vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            result.SetVertices(vertices);
            if (normals.Count == vertices.Count)
            {
                result.SetNormals(normals);
            }

            if (tangents.Count == vertices.Count)
            {
                result.SetTangents(tangents);
            }

            if (colors.Count == vertices.Count)
            {
                result.SetColors(colors);
            }

            if (uv0.Count == vertices.Count)
            {
                result.SetUVs(0, uv0);
            }

            if (uv1.Count == vertices.Count)
            {
                result.SetUVs(1, uv1);
            }

            result.subMeshCount = submeshes.Count;
            for (int s = 0; s < submeshes.Count; s++)
            {
                result.SetTriangles(submeshes[s], s);
            }

            result.RecalculateBounds();
            return result;
        }

        /// <summary>The longest gap along the road (z) between two neighbouring distinct rings of vertices, in metres.</summary>
        public static float LongestRingGap(Mesh mesh)
        {
            var levels = new SortedSet<int>();
            foreach (Vector3 v in mesh.vertices)
            {
                levels.Add(Mathf.RoundToInt(v.z * 1000f));
            }

            float longest = 0f;
            int previous = 0;
            bool first = true;
            foreach (int level in levels)
            {
                if (!first)
                {
                    longest = Mathf.Max(longest, (level - previous) / 1000f);
                }

                previous = level;
                first = false;
            }

            return longest;
        }

        /// <summary>Whether every vertex is on the first or the last ring (an extrusion along z), and where those are.</summary>
        private static bool TryRings(List<Vector3> vertices, out float start, out float end)
        {
            start = float.MaxValue;
            end = float.MinValue;
            foreach (Vector3 v in vertices)
            {
                start = Mathf.Min(start, v.z);
                end = Mathf.Max(end, v.z);
            }

            foreach (Vector3 v in vertices)
            {
                if (Mathf.Abs(v.z - start) > RingTolerance && Mathf.Abs(v.z - end) > RingTolerance)
                {
                    return false;
                }
            }

            return end - start > RingTolerance;
        }

        private static void SliceTriangle(List<int> output, int a, int b, int c, int slices, List<Vector3> vertices, List<Vector3> normals, List<Vector4> tangents, List<Color> colors, List<Vector2> uv0, List<Vector2> uv1, Dictionary<(int, int, int), int> cuts)
        {
            float za = vertices[a].z;
            float zb = vertices[b].z;
            float zc = vertices[c].z;
            bool ab = Mathf.Abs(za - zb) < RingTolerance;
            bool bc = Mathf.Abs(zb - zc) < RingTolerance;
            bool ca = Mathf.Abs(zc - za) < RingTolerance;
            if (ab && bc)
            {
                Add(output, a, b, c); // A cap, lying in one ring: nothing to cut.
                return;
            }

            // Turn the triangle (keeping its winding) so the base edge p-q lies on one ring and the apex on the other.
            int p, q, apex;
            if (ab)
            {
                p = a;
                q = b;
                apex = c;
            }
            else if (bc)
            {
                p = b;
                q = c;
                apex = a;
            }
            else
            {
                p = c;
                q = a;
                apex = b;
            }

            int previousP = p;
            int previousQ = q;
            for (int slice = 1; slice < slices; slice++)
            {
                int nextP = Cut(p, apex, slice, slices, vertices, normals, tangents, colors, uv0, uv1, cuts);
                int nextQ = Cut(q, apex, slice, slices, vertices, normals, tangents, colors, uv0, uv1, cuts);
                Add(output, previousP, previousQ, nextQ);
                Add(output, previousP, nextQ, nextP);
                previousP = nextP;
                previousQ = nextQ;
            }

            Add(output, previousP, previousQ, apex);
        }

        private static void Add(List<int> output, int a, int b, int c)
        {
            output.Add(a);
            output.Add(b);
            output.Add(c);
        }

        /// <summary>
        /// The vertex <paramref name="slice"/> slices of the way from <paramref name="from"/> to <paramref name="to"/>, made once per
        /// edge and place: two triangles that share the edge may name its ends the other way round, so the place is counted from the
        /// end with the lower index.
        /// </summary>
        private static int Cut(int from, int to, int slice, int slices, List<Vector3> vertices, List<Vector3> normals, List<Vector4> tangents, List<Color> colors, List<Vector2> uv0, List<Vector2> uv1, Dictionary<(int, int, int), int> cuts)
        {
            int a = Math.Min(from, to);
            int b = Math.Max(from, to);
            int step = from < to ? slice : slices - slice;
            (int, int, int) key = (a, b, step);
            if (cuts.TryGetValue(key, out int existing))
            {
                return existing;
            }

            float along = (float)step / slices;
            int index = vertices.Count;
            vertices.Add(Vector3.Lerp(vertices[a], vertices[b], along));
            if (normals.Count > b)
            {
                normals.Add(Vector3.Lerp(normals[a], normals[b], along).normalized);
            }

            if (tangents.Count > b)
            {
                Vector4 ta = tangents[a];
                Vector4 tb = tangents[b];
                Vector3 direction = Vector3.Lerp(ta, tb, along).normalized;
                tangents.Add(new Vector4(direction.x, direction.y, direction.z, ta.w));
            }

            if (colors.Count > b)
            {
                colors.Add(Color.Lerp(colors[a], colors[b], along));
            }

            if (uv0.Count > b)
            {
                uv0.Add(Vector2.Lerp(uv0[a], uv0[b], along));
            }

            if (uv1.Count > b)
            {
                uv1.Add(Vector2.Lerp(uv1[a], uv1[b], along));
            }

            cuts.Add(key, index);
            return index;
        }
    }
}
