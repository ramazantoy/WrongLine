using System;

namespace WrongLine.Attract
{
    /// <summary>The numbers behind the autopilot that drives the menu's background. Read every frame, so they can be tuned while the game runs.</summary>
    [Serializable]
    public sealed class AutopilotSettings
    {
        /// <summary>
        /// Where in the stretch of road where two clear lanes overlap the car starts to change lane, as a share of that stretch
        /// (0 at its start, 1 at its end). Early enough that the change is over well before the old lane fills up again.
        /// </summary>
        public float ChangeFraction = 0.15f;

        /// <summary>Held direction per metre still to go across the road, clamped to [-1, 1]: the car eases into the lane's middle.</summary>
        public float AxisGain = 2f;

        public void Validate()
        {
            Require(ChangeFraction >= 0f && ChangeFraction <= 0.5f, "ChangeFraction must be in [0, 0.5].");
            Require(AxisGain > 0f, "AxisGain must be positive.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new ArgumentException(message);
            }
        }
    }
}
