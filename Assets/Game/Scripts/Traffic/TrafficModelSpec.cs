namespace WrongLine.Traffic
{
    /// <summary>What the layout needs to know of a traffic car model: its length and how often it comes up.</summary>
    public readonly struct TrafficModelSpec
    {
        public readonly float Length;
        public readonly float Weight;

        public TrafficModelSpec(float length, float weight)
        {
            Length = length;
            Weight = weight;
        }
    }
}
