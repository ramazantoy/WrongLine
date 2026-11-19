using UnityEngine;

namespace WrongLine.UI
{
    /// <summary>The loading screen's roads: they fill towards the middle and draw back again, over and over.</summary>
    public static class LoadingWave
    {
        /// <summary>
        /// How much of the roads shows <paramref name="seconds"/> after the cover came up: 0 empty, 1 full. It takes
        /// <paramref name="wayDuration"/> seconds to fill and as long to empty, easing at both ends.
        /// </summary>
        public static float Fill(float seconds, float wayDuration)
        {
            if (wayDuration <= 0f)
            {
                return 0f;
            }

            float u = Mathf.Repeat(seconds / wayDuration, 2f);
            float t = u <= 1f ? u : 2f - u;
            return t * t * (3f - 2f * t);
        }
    }
}
