using UnityEngine;

namespace WrongLine.World
{
    /// <summary>
    /// Culling bounds for meshes drawn with the curved shader. A bent world pulls vertices away from where the mesh says they
    /// are; without a margin a renderer could be culled while part of it is still on screen.
    /// </summary>
    public static class CurvedBounds
    {
        /// <summary>
        /// Makes the renderer's bounds <paramref name="margin"/> bigger on every side, measured from the mesh's own bounds (so
        /// calling it again does not grow them). Only the renderer changes, never the shared mesh. A negative margin is none.
        /// </summary>
        public static void Expand(MeshRenderer renderer, MeshFilter filter, float margin)
        {
            if (filter == null || filter.sharedMesh == null)
            {
                return;
            }

            Bounds bounds = filter.sharedMesh.bounds;
            bounds.Expand(2f * Mathf.Max(0f, margin));
            renderer.localBounds = bounds;
        }
    }
}
