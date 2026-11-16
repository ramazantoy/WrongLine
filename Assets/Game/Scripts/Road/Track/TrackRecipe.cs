using System;

namespace WrongLine.Road
{
    /// <summary>How the heading changes along a piece. Banking follows the same shape.</summary>
    public enum YawProfile
    {
        /// <summary>One bend: the heading changes by the full yaw angle and stays there.</summary>
        Single,

        /// <summary>An S: the heading swings to the yaw angle in the first half and back to zero in the second.</summary>
        S,
    }

    /// <summary>How the slope changes along a piece.</summary>
    public enum PitchProfile
    {
        /// <summary>One ramp: the slope rises to the peak angle and falls back, so the piece climbs (or drops) overall.</summary>
        Bump,

        /// <summary>A hill (or dip): the slope goes up then down, so the piece ends at its entry height.</summary>
        Wave,
    }

    /// <summary>
    /// The parameters a road piece is baked from. Every piece starts and ends flat, level and straight (zero curvature), so
    /// any piece joins any other; the curve is opened and closed by smooth profiles inside the piece.
    /// </summary>
    public readonly struct TrackRecipe
    {
        public TrackRecipe(float length, float yawDegrees, YawProfile yawProfile, float pitchDegrees, PitchProfile pitchProfile, float bankDegrees)
        {
            Length = length;
            YawDegrees = yawDegrees;
            YawProfile = yawProfile;
            PitchDegrees = pitchDegrees;
            PitchProfile = pitchProfile;
            BankDegrees = bankDegrees;
        }

        /// <summary>Length along the centre line, in metres. A whole multiple of <see cref="TrackIntegrator.Step"/>.</summary>
        public float Length { get; }

        /// <summary>Heading change (<see cref="YawProfile.Single"/>) or peak heading (<see cref="YawProfile.S"/>). Positive turns right.</summary>
        public float YawDegrees { get; }

        public YawProfile YawProfile { get; }

        /// <summary>Peak slope angle. Positive climbs. For <see cref="PitchProfile.Wave"/> it scales a smooth up-then-down shape.</summary>
        public float PitchDegrees { get; }

        public PitchProfile PitchProfile { get; }

        /// <summary>Peak roll at the tightest point of the bend. Positive lowers the right edge (banks into a right turn).</summary>
        public float BankDegrees { get; }

        /// <summary>
        /// A hash of the shape parameters that is the same in every process (unlike <c>System.HashCode</c>), so a baked mesh can
        /// record what it was built from and a validator can tell when the recipe has moved on.
        /// </summary>
        public int StableHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Bits(Length);
                hash = hash * 31 + Bits(YawDegrees);
                hash = hash * 31 + (int)YawProfile;
                hash = hash * 31 + Bits(PitchDegrees);
                hash = hash * 31 + (int)PitchProfile;
                hash = hash * 31 + Bits(BankDegrees);
                return hash;
            }
        }

        // -0 and +0 are the same angle but have different bits (a mirrored piece with no bank is -0), and a serialisation
        // round trip may drop the sign.
        private static int Bits(float value)
        {
            return BitConverter.SingleToInt32Bits(value == 0f ? 0f : value);
        }

        public static TrackRecipe Straight(float length)
        {
            return new TrackRecipe(length, 0f, YawProfile.Single, 0f, PitchProfile.Bump, 0f);
        }
    }
}
