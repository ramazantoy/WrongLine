using System;

namespace WrongLine.Driving
{
    [Serializable]
    public sealed class ChaseCameraSettings
    {
        /// <summary>How far behind the car the camera sits, along the road, in metres.</summary>
        public float Distance = 8f;

        public float Height = 4.5f;

        /// <summary>The camera looks at the road this far ahead of the car, so it turns into a bend before the car does.</summary>
        public float LookAhead = 30f;

        /// <summary>
        /// The camera's look height is kept within this many degrees of the car's, so the car never drops out of the picture
        /// on a steep slope (the look point is far ahead; there the road may point quite differently from the car's own height).
        /// </summary>
        public float MaxCarPitchDegrees = 24f;

        /// <summary>
        /// The same sideways: the camera never turns further than this from the direction of the car, so a sharp bend does not
        /// push the car out of the (narrow, portrait) picture.
        /// </summary>
        public float MaxCarYawDegrees = 18f;

        /// <summary>0 keeps the camera's up level with the world; 1 leans it with the banked road.</summary>
        public float UpBlend = 0.5f;

        /// <summary>How much of the car's sideways position the camera takes up (0 stays on the centre line, 1 follows exactly).</summary>
        public float FollowX = 0.7f;

        /// <summary>The same for the point the camera looks at.</summary>
        public float LookFollowX = 0.5f;

        /// <summary>How quickly the camera's sideways position catches up (per second). The only smoothing there is: along the road the camera is exact.</summary>
        public float LateralSharpness = 8f;

        /// <summary>Field of view at <see cref="FovMinSpeed"/> and below, and at <see cref="FovMaxSpeed"/> and above, in degrees.</summary>
        public float BaseFov = 60f;

        public float MaxFov = 70f;

        public float FovMinSpeed = 12f;

        public float FovMaxSpeed = 30f;

        public void Validate()
        {
            Require(Distance >= 0f && Height >= 0f && LookAhead >= 0f, "Distance, Height and LookAhead must not be negative.");
            Require(MaxCarPitchDegrees > 0f && MaxCarPitchDegrees < 90f && MaxCarYawDegrees > 0f && MaxCarYawDegrees < 90f, "MaxCarPitchDegrees and MaxCarYawDegrees must be in (0, 90).");
            Require(UpBlend >= 0f && UpBlend <= 1f, "UpBlend must be in [0, 1].");
            Require(FollowX >= 0f && FollowX <= 1f && LookFollowX >= 0f && LookFollowX <= 1f, "FollowX and LookFollowX must be in [0, 1].");
            Require(LateralSharpness > 0f, "LateralSharpness must be positive.");
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
