using System;

namespace WrongLine.Crash
{
    /// <summary>The numbers behind collisions, the crash and what follows it. Read every frame, so they can be tuned while the game runs.</summary>
    [Serializable]
    public sealed class CrashSettings
    {
        [Serializable]
        public sealed class HitboxGroup
        {
            /// <summary>Half the player's box along the road. Must not be longer than the traffic planner assumes (see <see cref="Validate"/>).</summary>
            public float PlayerHalfLength = 2.1f;

            public float PlayerHalfWidth = 0.85f;

            /// <summary>Traffic boxes are this share of the car's size: a little forgiving.</summary>
            public float TrafficScale = 0.92f;

            /// <summary>Longest step, in metres of movement between two cars, the collision check takes between frames.</summary>
            public float StepMeters = 0.25f;
        }

        [Serializable]
        public sealed class SideswipeGroup
        {
            /// <summary>A touch at most this deep across the road (and along the side of the other car) is a sideswipe; deeper is a crash.</summary>
            public float MaxDepth = 0.35f;

            public float OncomingMaxDepth = 0.35f;

            /// <summary>The player is pushed away from the car this fast and this far, in metres per second and metres.</summary>
            public float PushSpeed = 5f;

            public float PushMeters = 0.8f;

            /// <summary>The visual kick of the car's body, in degrees, and how long it takes to fade, in seconds.</summary>
            public float KickYawDegrees = 6f;

            public float KickRollDegrees = 4f;

            public float KickSeconds = 0.35f;
        }

        [Serializable]
        public sealed class SlowMotionGroup
        {
            /// <summary>The game's speed after a crash, as a share of normal, and the real seconds it takes to get there.</summary>
            public float Scale = 0.25f;

            public float EaseSeconds = 0.1f;

            /// <summary>Real seconds from the crash to the game over screen.</summary>
            public float GameOverSeconds = 2.5f;
        }

        [Serializable]
        public sealed class WreckGroup
        {
            public float BodyMass = 900f;

            public float PartMass = 25f;

            /// <summary>Share of the car's speed the parts fly off with, and the extra random sideways and upward speed, in metres per second.</summary>
            public float SpeedShare = 0.6f;

            public float Spread = 4f;

            public float UpKick = 3f;

            /// <summary>Greatest spin of a part, in degrees per second.</summary>
            public float SpinDegrees = 360f;

            /// <summary>Force a door, hood or trunk hinge holds before it breaks off.</summary>
            public float BreakForce = 3000f;

            /// <summary>The temporary ground the parts land on: this many boxes of this length (metres) around the crash.</summary>
            public int GroundSegments = 12;

            public float GroundSegmentLength = 8f;
        }

        [Serializable]
        public sealed class CameraGroup
        {
            /// <summary>How fast the camera moves along the road to keep the wreck in the picture, and how fast it turns to look at it (per second).</summary>
            public float FollowSharpness = 3f;

            public float LookSharpness = 6f;
        }

        public HitboxGroup Hitbox = new HitboxGroup();
        public SideswipeGroup Sideswipe = new SideswipeGroup();
        public SlowMotionGroup SlowMotion = new SlowMotionGroup();
        public WreckGroup Wreck = new WreckGroup();
        public CameraGroup Camera = new CameraGroup();

        /// <param name="plannerPlayerLength">The player's length the traffic was laid out for; a longer box here would break its guarantee.</param>
        public void Validate(float plannerPlayerLength)
        {
            Require(Hitbox.PlayerHalfLength > 0f && Hitbox.PlayerHalfWidth > 0f, "Hitbox: the player's half length and width must be positive.");
            Require(Hitbox.PlayerHalfLength * 2f <= plannerPlayerLength, "Hitbox: the player's box must not be longer than the traffic planner's PlayerLengthMeters.");
            Require(Hitbox.TrafficScale > 0f && Hitbox.TrafficScale <= 1f && Hitbox.StepMeters > 0f, "Hitbox: TrafficScale in (0, 1], StepMeters positive.");
            Require(Sideswipe.MaxDepth >= 0f && Sideswipe.OncomingMaxDepth >= 0f && Sideswipe.PushSpeed >= 0f && Sideswipe.PushMeters >= 0f, "Sideswipe: depths, push speed and push distance must not be negative.");
            Require(Sideswipe.KickYawDegrees >= 0f && Sideswipe.KickRollDegrees >= 0f && Sideswipe.KickSeconds > 0f, "Sideswipe: kick angles must not be negative and KickSeconds positive.");
            Require(SlowMotion.Scale > 0f && SlowMotion.Scale <= 1f && SlowMotion.EaseSeconds >= 0f && SlowMotion.GameOverSeconds > SlowMotion.EaseSeconds, "SlowMotion: Scale in (0, 1], GameOverSeconds longer than EaseSeconds.");
            Require(Wreck.BodyMass > 0f && Wreck.PartMass > 0f && Wreck.BreakForce > 0f && Wreck.SpeedShare >= 0f && Wreck.Spread >= 0f && Wreck.UpKick >= 0f && Wreck.SpinDegrees >= 0f, "Wreck: masses and break force must be positive, the rest not negative.");
            Require(Wreck.GroundSegments >= 1 && Wreck.GroundSegmentLength > 0f, "Wreck: the ground needs at least one box of positive length.");
            Require(Camera.FollowSharpness > 0f && Camera.LookSharpness > 0f, "Camera: sharpness must be positive.");
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
