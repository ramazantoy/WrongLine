using System;

namespace WrongLine.Attract
{
    /// <summary>The numbers behind the first-person camera of the menu's background drive. Read every frame, so they can be tuned while the game runs.</summary>
    [Serializable]
    public sealed class FirstPersonCameraSettings
    {
        /// <summary>The driver's eye above the car's origin (the road under its wheels), in metres.</summary>
        public float EyeHeight = 1.15f;

        /// <summary>How far ahead of the car's origin the eye is, along the car, in metres.</summary>
        public float EyeForward = 0.3f;

        /// <summary>The camera looks this many degrees down from the car's own direction.</summary>
        public float PitchDownDegrees = 2f;

        /// <summary>
        /// Field of view at <see cref="FovMinSpeed"/> and below and at <see cref="FovMaxSpeed"/> and above, in degrees. It is the vertical
        /// one and the screen is portrait, so it is wide for a driver's view.
        /// </summary>
        public float BaseFov = 72f;

        public float MaxFov = 78f;

        public float FovMinSpeed = 12f;

        public float FovMaxSpeed = 30f;

        public void Validate()
        {
            Require(EyeHeight >= 0f && EyeForward >= 0f, "EyeHeight and EyeForward must not be negative.");
            Require(PitchDownDegrees > -90f && PitchDownDegrees < 90f, "PitchDownDegrees must be in (-90, 90).");
            Require(BaseFov > 0f && MaxFov >= BaseFov && MaxFov < 180f, "The field of view must satisfy 0 < BaseFov <= MaxFov < 180.");
            Require(FovMinSpeed >= 0f && FovMaxSpeed > FovMinSpeed, "FovMaxSpeed must be above FovMinSpeed.");
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
