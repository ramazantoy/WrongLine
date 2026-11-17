using UnityEngine;

namespace WrongLine.World
{
    /// <summary>
    /// How strongly a street lamp lights a point of the road under it: the cheap stand-in for a real point light. The same
    /// formula lives in CurvedLit.shader (<c>WL_LampPool</c>): change one, change the other.
    /// </summary>
    public static class LampPoolFalloff
    {
        /// <summary>The most lamp pools the shader takes at once (the size of its array).</summary>
        public const int MaxPools = 8;

        /// <param name="horizontalDistance">Metres from the lamp head's spot straight above, across the ground.</param>
        /// <param name="drop">Metres the point lies below the lamp head (negative: above it).</param>
        /// <param name="radius">Where the pool ends across the ground.</param>
        /// <param name="vertical">How far below the head the pool still reaches.</param>
        /// <returns>0 (no light) to 1 (full), softest at the rim: a squared linear falloff, with one metre of soft edge in height.</returns>
        public static float Weight(float horizontalDistance, float drop, float radius, float vertical)
        {
            float across = Mathf.Clamp01(1f - horizontalDistance / radius);
            float window = Mathf.Clamp01(drop) * Mathf.Clamp01(vertical - drop);
            return across * across * window;
        }
    }
}
