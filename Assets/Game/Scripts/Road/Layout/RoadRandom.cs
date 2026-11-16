namespace WrongLine.Road
{
    /// <summary>
    /// A small seeded generator (SplitMix64) so the same seed gives the same road on every platform and Unity version;
    /// <c>System.Random</c> and <c>UnityEngine.Random</c> promise neither.
    /// </summary>
    public sealed class RoadRandom
    {
        private ulong _state;

        public RoadRandom(ulong seed)
        {
            _state = seed;
        }

        public ulong NextUInt64()
        {
            unchecked
            {
                _state += 0x9E3779B97F4A7C15UL;
                ulong z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>A value in [0, 1).</summary>
        public double NextDouble()
        {
            return (NextUInt64() >> 11) * (1.0 / (1UL << 53));
        }
    }
}
