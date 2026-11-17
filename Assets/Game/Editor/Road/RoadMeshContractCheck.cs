using System.Collections.Generic;
using UnityEngine;

namespace WrongLine.Road.Editor
{
    /// <summary>
    /// Checks that a mesh is a valid body for a road piece, whoever made it. It looks only at where the vertices are, not at
    /// how the mesh is built: every vertex is projected onto the piece's centre line (distance along it <c>s</c>, sideways
    /// <c>x</c>, height <c>y</c>), so a mesh from Blender with other ring spacing, vertex order or topology passes when its
    /// shape is right. The mesh must be in the piece's local space (pivot at the entry frame).
    /// </summary>
    public static class RoadMeshContractCheck
    {
        /// <summary>How far the ends may be off, in metres (1 cm).</summary>
        public const float EndTolerance = 0.01f;

        public const float SurfaceTolerance = 0.02f;
        private const float UvTolerance = 0.01f;
        private const float FacingMinimum = 0.5f;
        private const float OuterX = RoadSection.HalfWidth + RoadSection.CurbWidth;

        private readonly struct Projected
        {
            public readonly float S;
            public readonly float X;
            public readonly float Y;
            public readonly Vector3 Up;

            public Projected(float s, float x, float y, Vector3 up)
            {
                S = s;
                X = x;
                Y = y;
                Up = up;
            }
        }

        /// <returns>What is wrong with the mesh, one line per kind of problem; empty when it is fine.</returns>
        public static List<string> Check(Mesh mesh, TrackShape shape)
        {
            var problems = new List<string>();
            if (mesh.subMeshCount != RoadMeshBuilder.SubmeshCount)
            {
                problems.Add($"it has {mesh.subMeshCount} submeshes, the contract has {RoadMeshBuilder.SubmeshCount} (asphalt, curb, line, body, in that order)");
                return problems;
            }

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            mesh.GetVertices(vertices);
            mesh.GetNormals(normals);
            mesh.GetUVs(0, uvs);
            if (vertices.Count == 0)
            {
                problems.Add("it has no vertices");
                return problems;
            }

            var projected = new Projected[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                projected[i] = Project(shape, vertices[i]);
            }

            CheckExtent(projected, shape, problems);
            CheckAsphalt(mesh, projected, normals, uvs, vertices, shape, problems);
            CheckSubmeshRoles(mesh, projected, problems);
            CheckLineUvs(mesh, projected, uvs, problems);
            return problems;
        }

        // The line texture is painted across u: solid yellow in the middle, dashed white on the other lines. A line's colour
        // therefore comes from its u, which must follow the asphalt's (u across the road, v along it), like every other part.
        private static void CheckLineUvs(Mesh mesh, Projected[] projected, List<Vector2> uvs, List<string> problems)
        {
            int[] indices = mesh.GetIndices(RoadMeshBuilder.SubmeshLine);
            if (indices.Length < 3 || uvs.Count != projected.Length)
            {
                return; // An empty line is reported by the role check, missing UVs by the asphalt check.
            }

            float worstU = 0f;
            float worstV = 0f;
            foreach (int i in new HashSet<int>(indices))
            {
                float expectedU = (projected[i].X + RoadSection.HalfWidth) / (2f * RoadSection.HalfWidth);
                worstU = Mathf.Max(worstU, Mathf.Abs(uvs[i].x - expectedU));
                worstV = Mathf.Max(worstV, Mathf.Abs(uvs[i].y - projected[i].S / RoadSection.TileLength));
            }

            if (worstU > UvTolerance || worstV > UvTolerance)
            {
                problems.Add($"the lane line UVs do not follow the road: u must be (sideways + {RoadSection.HalfWidth:F1}) / {2f * RoadSection.HalfWidth:F1} and v the distance along the piece over {RoadSection.TileLength:F0} m, but u is off by up to {worstU:F3} and v by up to {worstV:F3} (the line colour is read from u)");
            }
        }

        // The prefab gives submesh i the material of role i, so a mesh whose curb and line submeshes are swapped would draw
        // the line material on the curb: each submesh must be where its role is, judged by shape alone.
        private static void CheckSubmeshRoles(Mesh mesh, Projected[] projected, List<string> problems)
        {
            const float roleTolerance = 0.15f;
            float lineReach = RoadSection.LineWidth + roleTolerance;

            for (int submesh = RoadMeshBuilder.SubmeshCurb; submesh <= RoadMeshBuilder.SubmeshBody; submesh++)
            {
                int[] indices = mesh.GetIndices(submesh);
                string role = submesh == RoadMeshBuilder.SubmeshCurb ? "curb" : submesh == RoadMeshBuilder.SubmeshLine ? "line" : "body";
                if (indices.Length < 3)
                {
                    problems.Add($"the {role} submesh (number {submesh + 1}) is empty");
                    continue;
                }

                float minAbsX = float.MaxValue;
                float maxAbsX = 0f;
                float minY = float.MaxValue;
                float maxY = float.MinValue;
                var lineHit = new bool[RoadSection.LineCount];
                bool strayLineVertex = false;
                foreach (int i in new HashSet<int>(indices))
                {
                    if (submesh == RoadMeshBuilder.SubmeshLine)
                    {
                        int nearest = NearestLine(projected[i].X);
                        if (Mathf.Abs(projected[i].X - RoadSection.LineX(nearest)) <= lineReach)
                        {
                            lineHit[nearest] = true;
                        }
                        else
                        {
                            strayLineVertex = true;
                        }
                    }

                    float absX = Mathf.Abs(projected[i].X);
                    minAbsX = Mathf.Min(minAbsX, absX);
                    maxAbsX = Mathf.Max(maxAbsX, absX);
                    minY = Mathf.Min(minY, projected[i].Y);
                    maxY = Mathf.Max(maxY, projected[i].Y);
                }

                bool fits;
                string expected;
                if (submesh == RoadMeshBuilder.SubmeshCurb)
                {
                    fits = maxAbsX >= OuterX - roleTolerance && maxY >= RoadSection.CurbHeight - roleTolerance && minY >= -roleTolerance;
                    expected = $"reach the outer edge ({OuterX:F1} m from the centre), rise to {RoadSection.CurbHeight:F2} m and not hang below the surface";
                }
                else if (submesh == RoadMeshBuilder.SubmeshLine)
                {
                    fits = !strayLineVertex && System.Array.TrueForAll(lineHit, hit => hit) && maxY <= RoadSection.LineHeight + roleTolerance;
                    expected = $"lie in {RoadSection.LineCount} strips, one on each lane line ({LineList()} m from the centre), near the surface";
                }
                else
                {
                    fits = minY <= -RoadSection.Thickness + roleTolerance;
                    expected = $"hang down to {-RoadSection.Thickness:F1} m";
                }

                if (!fits)
                {
                    problems.Add($"submesh {submesh + 1} is not the {role}: it spans {minAbsX:F2} to {maxAbsX:F2} m sideways and {minY:F2} to {maxY:F2} m high, the {role} must {expected} (the submeshes go asphalt, curb, line, body, in that order)");
                }
            }
        }

        private static int NearestLine(float x)
        {
            int nearest = 0;
            for (int i = 1; i < RoadSection.LineCount; i++)
            {
                if (Mathf.Abs(x - RoadSection.LineX(i)) < Mathf.Abs(x - RoadSection.LineX(nearest)))
                {
                    nearest = i;
                }
            }

            return nearest;
        }

        private static string LineList()
        {
            var positions = new string[RoadSection.LineCount];
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = RoadSection.LineX(i).ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            }

            return string.Join(", ", positions);
        }

        private static void CheckExtent(Projected[] projected, TrackShape shape, List<string> problems)
        {
            float minS = float.MaxValue;
            float maxS = float.MinValue;
            float maxAbsX = 0f;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            foreach (Projected p in projected)
            {
                minS = Mathf.Min(minS, p.S);
                maxS = Mathf.Max(maxS, p.S);
                maxAbsX = Mathf.Max(maxAbsX, Mathf.Abs(p.X));
                minY = Mathf.Min(minY, p.Y);
                maxY = Mathf.Max(maxY, p.Y);
            }

            if (minS < -EndTolerance || maxS > shape.Length + EndTolerance)
            {
                problems.Add($"it reaches past the piece's ends: along the piece it spans {minS:F3} to {maxS:F3} m, the piece is 0 to {shape.Length:F3} m");
            }

            if (maxAbsX > OuterX + EndTolerance)
            {
                problems.Add($"it is wider than the section: {maxAbsX:F3} m from the centre line, at most {OuterX:F3} m");
            }

            if (maxY > RoadSection.CurbHeight + EndTolerance || minY < -RoadSection.Thickness - EndTolerance)
            {
                problems.Add($"it is outside the section's height: {minY:F3} to {maxY:F3} m, the section is {-RoadSection.Thickness:F3} to {RoadSection.CurbHeight:F3} m");
            }
        }

        private static void CheckAsphalt(Mesh mesh, Projected[] projected, List<Vector3> normals, List<Vector2> uvs, List<Vector3> vertices, TrackShape shape, List<string> problems)
        {
            int[] indices = mesh.GetIndices(RoadMeshBuilder.SubmeshAsphalt);
            if (indices.Length < 3)
            {
                problems.Add("the asphalt submesh (the first) is empty");
                return;
            }

            var used = new HashSet<int>(indices);
            float minS = float.MaxValue;
            float maxS = float.MinValue;
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float maxAbsY = 0f;
            float minU = float.MaxValue;
            float maxU = float.MinValue;
            float worstV = 0f;
            float worstNormal = 1f;
            bool hasUvs = uvs.Count == vertices.Count;
            bool hasNormals = normals.Count == vertices.Count;

            foreach (int i in used)
            {
                Projected p = projected[i];
                minS = Mathf.Min(minS, p.S);
                maxS = Mathf.Max(maxS, p.S);
                minX = Mathf.Min(minX, p.X);
                maxX = Mathf.Max(maxX, p.X);
                maxAbsY = Mathf.Max(maxAbsY, Mathf.Abs(p.Y));
                if (hasUvs)
                {
                    minU = Mathf.Min(minU, uvs[i].x);
                    maxU = Mathf.Max(maxU, uvs[i].x);
                    worstV = Mathf.Max(worstV, Mathf.Abs(uvs[i].y - p.S / RoadSection.TileLength));
                }

                if (hasNormals)
                {
                    worstNormal = Mathf.Min(worstNormal, Vector3.Dot(normals[i].normalized, p.Up));
                }
            }

            if (Mathf.Abs(minS) > EndTolerance || Mathf.Abs(maxS - shape.Length) > EndTolerance)
            {
                problems.Add($"the asphalt does not run from the entry to the exit: it spans {minS:F3} to {maxS:F3} m, it must span 0 to {shape.Length:F3} m (within {EndTolerance * 100f:F0} cm)");
            }

            if (Mathf.Abs(minX + RoadSection.HalfWidth) > EndTolerance || Mathf.Abs(maxX - RoadSection.HalfWidth) > EndTolerance)
            {
                problems.Add($"the asphalt is not {2f * RoadSection.HalfWidth:F1} m wide: it spans {minX:F3} to {maxX:F3} m sideways, it must span {-RoadSection.HalfWidth:F1} to {RoadSection.HalfWidth:F1} m");
            }

            if (maxAbsY > SurfaceTolerance)
            {
                problems.Add($"the asphalt surface is not at height 0: it is up to {maxAbsY:F3} m off (the contract allows {SurfaceTolerance:F2} m)");
            }

            if (!hasUvs)
            {
                problems.Add("it has no UVs");
            }
            else
            {
                if (Mathf.Abs(minU) > UvTolerance || Mathf.Abs(maxU - 1f) > UvTolerance)
                {
                    problems.Add($"the asphalt u does not run 0 to 1 across the road: it spans {minU:F3} to {maxU:F3}");
                }

                if (worstV > UvTolerance)
                {
                    problems.Add($"the asphalt v is not distance along the piece over {RoadSection.TileLength:F0} m: it is off by up to {worstV:F3}");
                }
            }

            if (hasNormals && worstNormal < FacingMinimum)
            {
                problems.Add("some asphalt normals do not face up");
            }

            CheckWinding(indices, vertices, projected, problems);
        }

        // Unity's front face is clockwise: the cross product of two edges points at the viewer. The asphalt must be seen from above.
        private static void CheckWinding(int[] indices, List<Vector3> vertices, Projected[] projected, List<string> problems)
        {
            int flipped = 0;
            int degenerate = 0;
            int triangles = indices.Length / 3;
            for (int t = 0; t < triangles; t++)
            {
                Vector3 a = vertices[indices[t * 3]];
                Vector3 b = vertices[indices[t * 3 + 1]];
                Vector3 c = vertices[indices[t * 3 + 2]];
                Vector3 facing = Vector3.Cross(b - a, c - a);
                if (facing.sqrMagnitude < 1e-12f)
                {
                    degenerate++;
                    continue;
                }

                if (Vector3.Dot(facing.normalized, projected[indices[t * 3]].Up) < FacingMinimum)
                {
                    flipped++;
                }
            }

            if (flipped > 0)
            {
                problems.Add($"{flipped} of {triangles} asphalt triangles face down (winding is the wrong way round: a mirrored or axis-swapped export needs its winding flipped)");
            }

            if (degenerate > 0)
            {
                problems.Add($"{degenerate} asphalt triangles have no area");
            }
        }

        private static Projected Project(TrackShape shape, Vector3 point)
        {
            int nearest = 0;
            float best = float.MaxValue;
            for (int i = 0; i < shape.Count; i++)
            {
                float distance = (point - shape[i].Position).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = i;
                }
            }

            TrackFrame frame = shape[nearest];
            Vector3 offset = point - frame.Position;
            float along = Vector3.Dot(offset, frame.Forward);
            return new Projected(nearest * shape.Step + along, Vector3.Dot(offset, frame.Right), Vector3.Dot(offset, frame.Up), frame.Up);
        }
    }
}
