namespace WrongLine.Traffic
{
    /// <summary>
    /// Which level of detail a car shows at a distance from the player: 0 close, 1 in the middle, 2 far. A car drops a level at the
    /// limit and goes back up only once it is <see cref="TrafficSettings.LodGroup.HysteresisMeters"/> inside it, so a car sitting at a
    /// limit does not flicker. Pure.
    /// </summary>
    public sealed class TrafficLodRule
    {
        public const int None = -1;

        private readonly TrafficSettings.LodGroup _settings;

        public TrafficLodRule(TrafficSettings.LodGroup settings)
        {
            _settings = settings;
        }

        /// <param name="current">The level shown now, or <see cref="None"/> for a car that was just put out.</param>
        public int Evaluate(float distance, int current)
        {
            int wanted = distance < _settings.NearMeters ? 0 : distance < _settings.FarMeters ? 1 : 2;
            if (current == None || wanted > current)
            {
                return wanted;
            }

            // Going back up: the distance must be inside the limit of the level above by the hysteresis.
            while (current > wanted)
            {
                float limit = current == 2 ? _settings.FarMeters : _settings.NearMeters;
                if (distance >= limit - _settings.HysteresisMeters)
                {
                    break;
                }

                current--;
            }

            return current;
        }
    }
}
