using System;

namespace WrongLine.World
{
    /// <summary>
    /// The numbers of the world bend. Everything further than <see cref="StartMeters"/> from the camera (measured along the
    /// ground) sinks by <see cref="Strength"/> times the extra distance squared, so the road curls away over the horizon.
    /// </summary>
    [Serializable]
    public sealed class CurveWorldSettings
    {
        /// <summary>Distance from the camera where the bend begins (metres). Nothing closer is bent.</summary>
        public float StartMeters = 30f;

        /// <summary>Metres of sinking per metre-squared beyond the start. 0 turns the bend off.</summary>
        public float Strength = 0.0004f;

        /// <summary>Where the fog starts, as a fraction of the road built ahead (see <see cref="FogRange"/>).</summary>
        public float FogStartFraction = 0.25f;

        /// <summary>
        /// The fog is fully thick this many metres before the end of the road built ahead, so the road is never seen to end.
        /// </summary>
        public float FogEndMargin = 40f;

        public void Validate()
        {
            if (!(FogStartFraction >= 0f && FogStartFraction < 1f))
            {
                throw new ArgumentException("FogStartFraction must be in [0, 1).");
            }

            if (!(FogEndMargin >= 0f))
            {
                throw new ArgumentException("FogEndMargin must not be negative.");
            }

            if (!(StartMeters >= 0f))
            {
                throw new ArgumentException("StartMeters must not be negative.");
            }

            if (!(Strength >= 0f))
            {
                throw new ArgumentException("Strength must not be negative.");
            }
        }
    }
}
