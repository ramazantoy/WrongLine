using UnityEngine;

namespace WrongLine.Road
{
    /// <summary>
    /// One sample of a road piece's centre line in the piece's local space: where the road is and how it is oriented.
    /// +Z of the rotation is the travel direction, +Y is the road's up (banking included).
    /// </summary>
    public readonly struct TrackFrame
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public TrackFrame(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public Vector3 Forward => Rotation * Vector3.forward;

        public Vector3 Up => Rotation * Vector3.up;

        public Vector3 Right => Rotation * Vector3.right;

        public static TrackFrame Lerp(in TrackFrame a, in TrackFrame b, float t)
        {
            return new TrackFrame(Vector3.Lerp(a.Position, b.Position, t), Quaternion.Slerp(a.Rotation, b.Rotation, t));
        }
    }
}
