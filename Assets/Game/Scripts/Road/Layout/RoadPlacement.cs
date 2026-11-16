using UnityEngine;

namespace WrongLine.Road
{
    /// <summary>Where a placed piece goes in the scene for a given floating origin. The piece prefab's pivot is its entry frame.</summary>
    public static class RoadPlacement
    {
        public static Vector3 Position(in PlacedPiece piece, in WorldPoint origin)
        {
            return piece.Entry.Position.RelativeTo(origin);
        }

        public static Quaternion Rotation(in PlacedPiece piece)
        {
            return Quaternion.Euler(0f, (float)piece.Entry.YawDegrees, 0f);
        }
    }
}
