namespace WrongLine.Traffic
{
    /// <summary>
    /// What the traffic needs to know of a car model: its length and how often it comes up (the layout), and where its wheels
    /// touch the road (the pose, see <see cref="WrongLine.Road.WheelPoseRule"/>). Axle positions are along the car's forward axis from its pivot.
    /// </summary>
    public readonly struct TrafficModelSpec
    {
        public readonly float Length;
        public readonly float Weight;
        public readonly float FrontAxle;
        public readonly float RearAxle;
        public readonly float HalfTrack;

        public TrafficModelSpec(float length, float weight)
            : this(length, weight, length * 0.3f, -length * 0.3f, 0.75f)
        {
        }

        public TrafficModelSpec(float length, float weight, float frontAxle, float rearAxle, float halfTrack)
        {
            Length = length;
            Weight = weight;
            FrontAxle = frontAxle;
            RearAxle = rearAxle;
            HalfTrack = halfTrack;
        }
    }
}
