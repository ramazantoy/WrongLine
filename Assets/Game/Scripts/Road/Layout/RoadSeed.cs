namespace WrongLine.Road
{
    public static class RoadSeed
    {
        /// <summary>The configured seed, or <paramref name="random"/> when the config says 0 ("a new road every run").</summary>
        public static ulong Resolve(int configured, ulong random)
        {
            return configured != 0 ? unchecked((ulong)(uint)configured) : random;
        }
    }
}
