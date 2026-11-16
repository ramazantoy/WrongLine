using UnityEngine;

namespace WrongLine.Driving
{
    /// <summary>
    /// The scene origin moved: every scene position decreased by <see cref="Shift"/>. Sent after the road and the follower
    /// have been placed again; whatever keeps its own scene position (the camera) shifts by the same amount.
    /// </summary>
    public readonly struct RoadOriginShifted
    {
        public readonly Vector3 Shift;

        public RoadOriginShifted(Vector3 shift)
        {
            Shift = shift;
        }
    }
}
