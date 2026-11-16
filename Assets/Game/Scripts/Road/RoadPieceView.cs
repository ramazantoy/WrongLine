using UnityEngine;

namespace WrongLine.Road
{
    /// <summary>
    /// The root of a road piece prefab. Holds the recipe the piece was built from and the hash of its parameters at build
    /// time, so a validator can tell a stale mesh. The pivot is the piece's entry frame.
    /// </summary>
    public sealed class RoadPieceView : MonoBehaviour
    {
        private const float GizmoStep = 2f;
        private const float GizmoUpLength = 1.5f;

        [SerializeField] private RoadPieceRecipe _recipe;
        [SerializeField] private int _bakedHash;

        public RoadPieceRecipe Recipe => _recipe;

        /// <summary>The recipe's <see cref="TrackRecipe.StableHash"/> when the mesh was last built.</summary>
        public int BakedHash => _bakedHash;

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
