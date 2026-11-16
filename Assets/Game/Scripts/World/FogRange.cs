using System;

namespace WrongLine.World
{
    /// <summary>
    /// Where the linear fog starts and ends, derived from how far ahead the road is built. The fog is fully thick before the
    /// last built road, so the end of the road is never seen; it also starts a fixed fraction in, so the near road stays clear.
    /// </summary>
    public static class FogRange
    {
        /// <exception cref="ArgumentException">The settings leave no room for a fog (it would end before it starts).</exception>
        public static (float Start, float End) Compute(CurveWorldSettings settings, float aheadMeters)
        {
            float end = aheadMeters - settings.FogEndMargin;
            float start = aheadMeters * settings.FogStartFraction;
            if (!(aheadMeters > 0f) || !(end > start))
            {
                throw new ArgumentException($"Fog would end at {end:F1} m but start at {start:F1} m (road built {aheadMeters:F1} m ahead): lower FogEndMargin or FogStartFraction.");
            }

            return (start, end);
        }
    }
}
