using System;

namespace WrongLine.Audio
{
    /// <summary>When a traffic car passing the player makes a whoosh, and how loud.</summary>
    [Serializable]
    public sealed class PassBySettings
    {
        /// <summary>A car further than this across the road (metres) does not count as passing: it is not in a lane beside the player.</summary>
        public float MaxSideMeters = 8f;

        /// <summary>A jump along the road bigger than this in one frame is not a pass but a car appearing or the road shifting.</summary>
        public float MaxStepMeters = 15f;

        /// <summary>The closing speed (metres per second) that makes the whoosh full volume; slower passes fade down to <see cref="MinVolume"/>.</summary>
        public float FullClosingSpeed = 50f;

        public float MinVolume = 0.25f;

        public void Validate()
        {
            if (MaxSideMeters <= 0f || MaxStepMeters <= 0f || FullClosingSpeed <= 0f || MinVolume < 0f || MinVolume > 1f)
            {
                throw new ArgumentException("PassBy: distances and speed must be positive and MinVolume in [0, 1].");
            }
        }
    }
}
