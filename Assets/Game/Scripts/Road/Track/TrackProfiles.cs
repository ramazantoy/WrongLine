using UnityEngine;

namespace WrongLine.Road
{
    /// <summary>
    /// The closed-form shapes a piece's heading, slope and roll follow over its normalised length <c>u</c> in [0, 1]. All of
    /// them are flat (zero value and zero rate) at both ends, so pieces join without a kink.
    /// </summary>
    internal static class TrackProfiles
    {
        private const float TwoPi = Mathf.PI * 2f;
        private const float FourPi = Mathf.PI * 4f;

        /// <summary>Fraction of the yaw angle reached at <paramref name="u"/> (the integral of the bend's curvature profile).</summary>
        public static float Yaw(YawProfile profile, float u)
        {
            if (profile == YawProfile.Single)
            {
                // Curvature 2 sin^2(pi u): the heading is u - sin(2 pi u) / (2 pi).
                return u - Mathf.Sin(TwoPi * u) / TwoPi;
            }

            // Curvature 4 sin^2(2 pi u), mirrored (negated) in the second half.
            if (u <= 0.5f)
            {
                return 2f * u - Mathf.Sin(FourPi * u) / TwoPi;
            }

            return 2f - 2f * u + Mathf.Sin(FourPi * u) / TwoPi;
        }

        /// <summary>Fraction of the peak slope angle at <paramref name="u"/>.</summary>
        public static float Pitch(PitchProfile profile, float u)
        {
            float bump = Mathf.Sin(Mathf.PI * u);
            bump *= bump;
            return profile == PitchProfile.Bump ? bump : bump * Mathf.Sin(TwoPi * u);
        }

        /// <summary>Fraction of the peak roll at <paramref name="u"/>: the shape of the bend's curvature, normalised to a peak of 1.</summary>
        public static float Bank(YawProfile profile, float u)
        {
            if (profile == YawProfile.Single)
            {
                float bump = Mathf.Sin(Mathf.PI * u);
                return bump * bump;
            }

            float s = Mathf.Sin(TwoPi * u);
            return u <= 0.5f ? s * s : -s * s;
        }
    }
}
