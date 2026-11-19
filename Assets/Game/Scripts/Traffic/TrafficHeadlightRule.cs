namespace WrongLine.Traffic
{
    /// <summary>
    /// The real headlight of a traffic car: lit at night like the player's, but weaker and only on the nearest levels of detail (each
    /// lit car is one more light for the renderer; the far cars keep the glowing panels of their light mask). Pure.
    /// </summary>
    public static class TrafficHeadlightRule
    {
        private const float OffBelow = 0.001f;

        /// <param name="headlightLevel">How lit the night lights are, 0 (day) to 1.</param>
        /// <param name="level">The car's level of detail, or <see cref="TrafficLodRule.None"/>.</param>
        /// <param name="playerIntensity">The player's headlight intensity: the traffic's is a share of it.</param>
        /// <returns>The light's intensity, 0 when it is to be off.</returns>
        public static float Intensity(float headlightLevel, int level, TrafficSettings.LightsGroup settings, float playerIntensity)
        {
            if (level < 0 || level > settings.MaxLevel || headlightLevel <= OffBelow)
            {
                return 0f;
            }

            return playerIntensity * settings.IntensityScale * headlightLevel;
        }

        public static float Range(float playerRange, TrafficSettings.LightsGroup settings)
        {
            return playerRange * settings.RangeScale;
        }
    }
}
