using AF.Core;
using UnityEngine;

namespace WrongLine.Road
{
    /// <summary>
    /// The root of a road piece prefab. Holds the recipe the piece was built from and the hash of its parameters at build
    /// time, so a validator can tell a stale mesh. The pivot is the piece's entry frame.
    /// While pooled it carries the index of the road piece it currently stands for; going back to the pool clears that and
    /// the transform, so a waiting piece holds nothing from its last use.
    /// </summary>
    public sealed class RoadPieceView : MonoBehaviour, IPoolable
    {
        /// <summary>The value of <see cref="PlacedIndex"/> while the piece is not in use.</summary>
        public const int NotPlaced = -1;

        private const float GizmoStep = 2f;
        private const float GizmoUpLength = 1.5f;

        [SerializeField] private RoadPieceRecipe _recipe;
        [SerializeField] private int _bakedHash;

        private MeshRenderer _renderer;
        private MeshFilter _filter;

        public RoadPieceRecipe Recipe => _recipe;

        /// <summary>The recipe's <see cref="TrackRecipe.StableHash"/> when the mesh was last built.</summary>
        public int BakedHash => _bakedHash;

        /// <summary>The index of the road piece this instance stands for, or <see cref="NotPlaced"/>.</summary>
        public int PlacedIndex { get; private set; } = NotPlaced;

        internal void Bind(int placedIndex)
        {
            PlacedIndex = placedIndex;
        }

        /// <summary>
        /// Makes the renderer's culling bounds this much bigger on every side, measured from the mesh's own bounds (so calling
        /// it again does not grow them). A bent world pulls vertices away from where the mesh says they are; without the
        /// margin a piece could be culled while part of it is still on screen. Only this renderer changes, never the shared mesh.
        /// </summary>
        public void ExpandBounds(float margin)
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<MeshRenderer>();
                _filter = GetComponent<MeshFilter>();
            }

            Bounds bounds = _filter.sharedMesh.bounds;
            bounds.Expand(2f * Mathf.Max(0f, margin));
            _renderer.localBounds = bounds;
        }

        void IPoolable.OnSpawned(PoolLife life)
        {
        }

        void IPoolable.OnDespawned()
        {
            PlacedIndex = NotPlaced;
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private void OnDrawGizmosSelected()
        {
            if (_recipe == null)
            {
                return;
            }

            TrackShape shape = _recipe.Shape;
            Vector3 previous = transform.TransformPoint(shape.Entry.Position);
            for (float s = GizmoStep; s < shape.Length + GizmoStep; s += GizmoStep)
            {
                TrackFrame frame = shape.Evaluate(s);
                Vector3 point = transform.TransformPoint(frame.Position);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(previous, point);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(point, point + transform.TransformDirection(frame.Up) * GizmoUpLength);
                previous = point;
            }

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.TransformPoint(shape.Exit.Position), 0.4f);
        }
    }
}
