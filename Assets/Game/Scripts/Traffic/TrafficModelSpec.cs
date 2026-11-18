namespace WrongLine.Traffic
{
    /// <summary>
    /// What the traffic needs to know of a car model: its length and how often it comes up (the layout), where its wheels
    /// touch the road (the pose, see <see cref="WrongLine.Road.WheelPoseRule"/>) and how wide it is (the crash check). Axle
    /// positions are along the car's forward axis from its pivot.
    /// </summary>
    public readonly struct TrafficModelSpec
    {
        /// <summary>Half the width used when a model does not say: a car's body, a little wider than its track.</summary>
        public const float DefaultHalfWidth = 0.9f;

        public readonly float Length;
        public readonly float Weight;
        public readonly float FrontAxle;
        public readonly float RearAxle;
        public readonly float HalfTrack;
        public readonly float HalfWidth;

        public TrafficModelSpec(float length, float weight)
            : this(length, weight, length * 0.3f, -length * 0.3f, 0.75f)
        {
        }

        public TrafficModelSpec(float length, float weight, float frontAxle, float rearAxle, float halfTrack)
            : this(length, weight, frontAxle, rearAxle, halfTrack, DefaultHalfWidth)
        {
        }

        public TrafficModelSpec(float length, float weight, float frontAxle, float rearAxle, float halfTrack, float halfWidth)
        {
            Length = length;
            Weight = weight;
            FrontAxle = frontAxle;
            RearAxle = rearAxle;
            HalfTrack = halfTrack;
            HalfWidth = halfWidth;
        }
    }
}
