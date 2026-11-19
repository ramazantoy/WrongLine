using System;

namespace WrongLine.Audio
{
    /// <summary>
    /// The numbers behind the engine's voice: a gearbox that only exists in the sound. The car's speed sweeps the revs up through
    /// each gear and drops them at the shift; the revs set the pitch and fade from the low loop to the high one. Read every frame.
    /// </summary>
    [Serializable]
    public sealed class EngineSoundSettings
    {
        /// <summary>The speed each gear tops out at, in metres per second, lowest first. The last one is the car's top speed.</summary>
        public float[] GearTopSpeeds = { 18f, 30f, 34f, 38f, 42f };

        /// <summary>The revs (0 to 1) at the bottom of a gear.</summary>
        public float IdleRevs = 0.3f;

        /// <summary>How fast the revs climb and fall, in revs (0 to 1) per second. The slower fall is the dip you hear at a shift.</summary>
        public float RiseRate = 3f;

        public float FallRate = 1.8f;

        /// <summary>The loops' pitch at 0 revs and at full revs.</summary>
        public float PitchAtIdle = 0.8f;

        public float PitchAtRedline = 1.7f;

        /// <summary>Between these revs the sound fades from the low loop to the high one.</summary>
        public float CrossfadeFrom = 0.35f;

        public float CrossfadeTo = 0.8f;

        /// <summary>Extra revs at full sideways speed (the engine works a little when the car changes lanes), and the sideways speed that counts as full.</summary>
        public float LateralRevs = 0.05f;

        public float LateralFullSpeed = 6f;

        public float MasterVolume = 0.7f;

        /// <summary>The volume share at a standstill, rising to 1 at <see cref="FullVolumeSpeed"/>.</summary>
        public float IdleVolume = 0.6f;

        public float FullVolumeSpeed = 30f;

        /// <summary>The road and wind rush: volume and pitch grow with speed up to <see cref="WindFullSpeed"/>.</summary>
        public float WindVolume = 0.35f;

        public float WindFullSpeed = 42f;

        public float WindPitchMin = 0.8f;

        public float WindPitchMax = 1.3f;

        /// <summary>How long the engine takes to die away after a crash, in game seconds.</summary>
        public float CrashFadeSeconds = 0.8f;

        public void Validate()
        {
            Require(GearTopSpeeds != null && GearTopSpeeds.Length > 0, "Engine: at least one gear.");
            float last = 0f;
            foreach (float top in GearTopSpeeds)
            {
                Require(top > last, "Engine: gear top speeds must rise and be positive.");
                last = top;
            }

            Require(IdleRevs >= 0f && IdleRevs < 1f, "Engine: IdleRevs must be in [0, 1).");
            Require(RiseRate > 0f && FallRate > 0f, "Engine: RiseRate and FallRate must be positive.");
            Require(PitchAtIdle > 0f && PitchAtRedline >= PitchAtIdle, "Engine: pitches must be positive and rise.");
            Require(CrossfadeTo > CrossfadeFrom, "Engine: CrossfadeTo must be above CrossfadeFrom.");
            Require(LateralFullSpeed > 0f && FullVolumeSpeed > 0f && WindFullSpeed > 0f, "Engine: the full speeds must be positive.");
            Require(CrashFadeSeconds > 0f, "Engine: CrashFadeSeconds must be positive.");
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
