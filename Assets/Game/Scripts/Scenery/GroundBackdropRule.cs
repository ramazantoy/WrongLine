using System;
using WrongLine.Road;

namespace WrongLine.Scenery
{
    /// <summary>
    /// How high the backdrop ground (a wide flat disc under the camera that fills the gaps beside and between the ground
    /// strips) sits: a little below the lowest road piece in the scene, so it can never cover the road. When a lower piece
    /// comes in it drops at once; when the low pieces leave it rises slowly, so the change is not seen.
    /// </summary>
    public static class GroundBackdropRule
    {
        /// <summary>The height to aim at: the lowest point of the pieces <paramref name="first"/> to <paramref name="last"/>, minus <paramref name="depth"/>.</summary>
        public static double Target(IRoadPath path, int first, int last, double depth)
        {
            double lowest = double.MaxValue;
            for (int index = Math.Max(first, path.FirstIndex); index <= last && index < path.EndIndex; index++)
            {
                PlacedPiece placed = path[index];
                lowest = Math.Min(lowest, placed.Entry.Y + placed.Piece.MinY);
            }

            return lowest == double.MaxValue ? -depth : lowest - depth;
        }

        /// <summary>The next height: down to the target at once, up towards it at most <paramref name="riseSpeed"/> metres a second.</summary>
        public static double Next(double current, double target, float deltaTime, float riseSpeed)
        {
            if (target <= current)
            {
                return target;
            }

            return Math.Min(target, current + Math.Max(0f, riseSpeed) * Math.Max(0f, deltaTime));
        }
    }
}
