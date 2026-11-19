using System;
using WrongLine.Traffic;

namespace WrongLine.Attract
{
    /// <summary>
    /// Drives the player's car along the lane the traffic planner keeps clear (<see cref="TrafficCorridor"/>), so it never touches
    /// another car. The corridor moves by one lane from slice to slice and, where two slices meet, both lanes are clear over a whole
    /// slice: the car changes lane early in that stretch and is in the new lane well before the old one fills up. Pure.
    /// </summary>
    public sealed class AutopilotRule
    {
        private readonly AutopilotSettings _settings;

        public AutopilotRule(AutopilotSettings settings)
        {
            settings.Validate();
            _settings = settings;
        }

        /// <summary>The lane the car should be in when it is <paramref name="sigma"/> metres along the road.</summary>
        public int LaneAt(TrafficCorridor corridor, double sigma)
        {
            // Slice k's lane takes over where slices k-1 and k begin to overlap (half a slice before slice k starts), a bit into it.
            double slices = (sigma - corridor.Start) / corridor.SliceLength + 0.5 - _settings.ChangeFraction;
            int index = Math.Max(0, (int)Math.Floor(slices));
            return corridor.LaneOf(index);
        }

        /// <summary>The held direction in [-1, 1] that moves the car's target from <paramref name="targetX"/> to <paramref name="desiredX"/>.</summary>
        public float Axis(float desiredX, float targetX)
        {
            float axis = (desiredX - targetX) * _settings.AxisGain;
            return axis < -1f ? -1f : axis > 1f ? 1f : axis;
        }
    }
}
