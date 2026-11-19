using UnityEngine;

namespace WrongLine.UI
{
    /// <summary>The loading screen's road: it fills from empty to full at a steady speed, then starts over from empty, until the load is done.</summary>
    public static class LoadingWave
    {
        /// <summary>
        /// How much of the road shows <paramref name="seconds"/> after the cover came up: 0 empty, 1 full. One fill takes
        /// <paramref name="fillDuration"/> seconds at a constant speed; right after it is full the road is empty again.
        /// </summary>
        public static float Fill(float seconds, float fillDuration)
        {
            if (fillDuration <= 0f)
            {
                return 0f;
            }

            return Mathf.Repeat(seconds / fillDuration, 1f);
        }
    }
}
