using System;

namespace WrongLine.Road
{
    /// <summary>The numbers the road generator's rules run on. A plain object so the rules stay testable; the config asset fills one in.</summary>
    [Serializable]
    public sealed class RoadGeneratorSettings
    {
        /// <summary>
        /// The road never heads more than this far from straight ahead, anywhere along it. With less than 90 degrees the
        /// road always moves forward, so it can never cross itself.
        /// </summary>
        public float MaxYawDegrees = 75f;

        /// <summary>The band the road stays inside, relative to where the run started (metres).</summary>
        public float MinHeight = -30f;

        public float MaxHeight = 60f;

        /// <summary>The distance over which the road ramps from calm to demanding.</summary>
        public float DifficultyRampMeters = 6000f;

        /// <summary>A piece is not picked again within this many picks of its last use (when there is any other choice).</summary>
        public int RepeatWindow = 2;

        /// <summary>How strongly a piece that steers back towards the middle of the yaw or height band is favoured.</summary>
        public float YawPull = 1f;

        public float HeightPull = 1f;

        /// <summary>How much a piece is liked, per tag. A piece with several tags gets the mean.</summary>
        public float WeightStraight = 3f;

        public float WeightCurve = 2f;
        public float WeightSCurve = 1.5f;
        public float WeightClimb = 1f;
        public float WeightDrop = 1f;
        public float WeightHill = 1f;
        public float WeightDip = 1f;
        public float WeightBanked = 1.5f;

        /// <summary>At the start the weight of a piece falls by this fraction of its intensity (0 = no preference for calm pieces).</summary>
        public float EasyIntensityPenalty = 0.8f;

        /// <summary>At full difficulty a piece's weight is this plus <see cref="HardGain"/> times its intensity.</summary>
        public float HardBase = 0.4f;

        public float HardGain = 1.2f;

        public void Validate()
        {
            Require(MaxYawDegrees > 0f && MaxYawDegrees < 90f, nameof(MaxYawDegrees), "must be between 0 and 90 degrees");
            Require(MinHeight < 0f && MaxHeight > 0f, nameof(MinHeight), "the height band must contain the start height 0");
            Require(DifficultyRampMeters > 0f, nameof(DifficultyRampMeters), "must be positive");
            Require(RepeatWindow >= 0, nameof(RepeatWindow), "must not be negative");
            Require(YawPull >= 0f && HeightPull >= 0f, nameof(YawPull), "pulls must not be negative");
            Require(EasyIntensityPenalty >= 0f && EasyIntensityPenalty < 1f, nameof(EasyIntensityPenalty), "must be in [0, 1)");
            Require(HardBase > 0f && HardGain >= 0f, nameof(HardBase), "must be positive (gain not negative)");
            foreach (float weight in new[] { WeightStraight, WeightCurve, WeightSCurve, WeightClimb, WeightDrop, WeightHill, WeightDip, WeightBanked })
            {
                Require(weight > 0f, "Weight*", "every tag weight must be positive");
            }
        }

        internal double WeightOf(RoadTags tags)
        {
            double sum = 0.0;
            int count = 0;
            Add(tags, RoadTags.Straight, WeightStraight, ref sum, ref count);
            Add(tags, RoadTags.Curve, WeightCurve, ref sum, ref count);
            Add(tags, RoadTags.SCurve, WeightSCurve, ref sum, ref count);
            Add(tags, RoadTags.Climb, WeightClimb, ref sum, ref count);
            Add(tags, RoadTags.Drop, WeightDrop, ref sum, ref count);
            Add(tags, RoadTags.Hill, WeightHill, ref sum, ref count);
            Add(tags, RoadTags.Dip, WeightDip, ref sum, ref count);
            Add(tags, RoadTags.Banked, WeightBanked, ref sum, ref count);
            return count == 0 ? 1.0 : sum / count;
        }

        private static void Add(RoadTags tags, RoadTags flag, float weight, ref double sum, ref int count)
        {
            if (tags.HasFlag(flag))
            {
                sum += weight;
                count++;
            }
        }

        private static void Require(bool condition, string name, string message)
        {
            if (!condition)
            {
                throw new ArgumentException($"{name}: {message}.");
            }
        }
    }
}
