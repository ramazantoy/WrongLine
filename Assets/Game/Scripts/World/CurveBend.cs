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

        /// <summary>
        /// How far below the horizon (as the sine of the angle, the sky shader's <c>direction.y</c> negated) the sky keeps the
        /// horizon colour: the angle at which the road end appears plus the configured margin. Below it the sky blends to the
        /// ground colour; above it a road fogged to the horizon colour has the same colour behind it.
        /// </summary>
        public static float HorizonHold(CurveWorldSettings settings, float cameraHeight, float roadEndGroundDistance)
        {
            double degrees = AngleBelowHorizon(settings, cameraHeight, roadEndGroundDistance) + settings.HorizonHoldMarginDegrees;
            return (float)Math.Sin(Math.Min(degrees, 89.0) * (Math.PI / 180.0));
        }
    }
}
