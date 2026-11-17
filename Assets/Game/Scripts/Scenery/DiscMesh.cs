using UnityEngine;

namespace WrongLine.Scenery
{
    /// <summary>
    /// A flat disc facing up, centred on the origin, with rings closer near the middle: the world bend moves vertices, so the
    /// disc needs them where it bends the most visibly (near the camera) and few far away.
    /// </summary>
    public static class DiscMesh
    {
        public static Mesh Build(float radius, int rings, int segments)
        {
            var vertices = new Vector3[1 + rings * segments];
            vertices[0] = Vector3.zero;
            for (int ring = 0; ring < rings; ring++)
            {
                float share = (ring + 1f) / rings;
                float r = radius * share * share; // Denser near the middle.
                for (int segment = 0; segment < segments; segment++)
                {
                    float angle = segment * Mathf.PI * 2f / segments;
                    vertices[1 + ring * segments + segment] = new Vector3(Mathf.Sin(angle) * r, 0f, Mathf.Cos(angle) * r);
                }
            }

            var triangles = new int[(segments + (rings - 1) * segments * 2) * 3];
            int t = 0;
            for (int segment = 0; segment < segments; segment++)
            {
                // The angle grows clockwise seen from above (towards +X from +Z), so centre, this, next faces up.
                triangles[t++] = 0;
                triangles[t++] = 1 + segment;
                triangles[t++] = 1 + (segment + 1) % segments;
            }

            for (int ring = 1; ring < rings; ring++)
            {
                for (int segment = 0; segment < segments; segment++)
                {
                    int inner = 1 + (ring - 1) * segments;
                    int outer = 1 + ring * segments;
                    int next = (segment + 1) % segments;
                    triangles[t++] = inner + segment;
                    triangles[t++] = outer + segment;
                    triangles[t++] = outer + next;
                    triangles[t++] = inner + segment;
                    triangles[t++] = outer + next;
                    triangles[t++] = inner + next;
                }
            }

            var normals = new Vector3[vertices.Length];
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = Vector3.up;
            }

            var mesh = new Mesh { name = "GroundBackdrop" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
