using System;

namespace WrongLine.Driving
{
    [Serializable]
    public sealed class ChaseCameraSettings
    {
        /// <summary>How far behind the follower the camera sits, along the road, in metres.</summary>
        public float Distance = 8f;

        public float Height = 4.5f;

        /// <summary>The camera looks at the point this far ahead of the follower.</summary>
        public float LookAhead = 14f;

        /// <summary>How quickly the camera catches up in position and in rotation (per second; higher is stiffer).</summary>
        public float PositionSharpness = 10f;

        public float RotationSharpness = 8f;

        /// <summary>0 keeps the camera's up level with the world; 1 leans it with the banked road.</summary>
        public float UpBlend = 0.5f;

        public void Validate()
        {
            Require(Distance >= 0f && Height >= 0f && LookAhead >= 0f, "Distance, Height and LookAhead must not be negative.");
            Require(PositionSharpness > 0f && RotationSharpness > 0f, "The sharpness values must be positive.");
            Require(UpBlend >= 0f && UpBlend <= 1f, "UpBlend must be in [0, 1].");
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
