using System;

namespace WrongLine.World
{
    /// <summary>
    /// The world bend as a rule. The shader include <c>CurveWorld.hlsl</c> carries the same formula: change one, change the
    /// other. The distance is the ground distance to the camera (radial, not along the view direction), so turning the
    /// camera never makes the world swim.
    /// </summary>
    public static class CurveBend
    {
        /// <summary>How far a point at this ground distance from the camera sinks (metres, not negative).</summary>
        public static float Drop(CurveWorldSettings settings, float groundDistance)
        {
            float beyond = Math.Max(0f, groundDistance - settings.StartMeters);
            return settings.Strength * beyond * beyond;
        }

        /// <summary>The most any vertex within this ground distance can sink; the margin a renderer's bounds need.</summary>
        public static float MaxDrop(CurveWorldSettings settings, float maxGroundDistance)
        {
            return Drop(settings, maxGroundDistance);
        }

        /// <summary>
        /// How far below the camera's horizontal a road point at this ground distance appears, in degrees, for a point on
        /// the ground with the camera <paramref name="cameraHeight"/> above it. Bigger than the flat-world angle: the bend
        /// pulls the road end down out of the horizon.
        /// </summary>
        public static float AngleBelowHorizon(CurveWorldSettings settings, float cameraHeight, float groundDistance)
        {
            float below = cameraHeight + Drop(settings, groundDistance);
            return (float)(Math.Atan2(below, groundDistance) * (180.0 / Math.PI));
        }
    }
}
