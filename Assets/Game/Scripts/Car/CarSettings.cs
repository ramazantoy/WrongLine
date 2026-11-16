using System;
using WrongLine.Road;

namespace WrongLine.Car
{
    /// <summary>The numbers behind the car's movement, posing and turn signal. Read every frame, so they can be tuned while the game runs.</summary>
    [Serializable]
    public sealed class CarSettings
    {
        [Serializable]
        public sealed class SpeedGroup
        {
            /// <summary>Speed at the start of a run and the speed it settles at, in metres per second (30 is 108 km/h).</summary>
            public float StartSpeed = 12f;

            public float CruiseSpeed = 30f;

            /// <summary>Metres per second gained per second until the cruise speed is reached.</summary>
            public float Acceleration = 8f;
        }

        [Serializable]
        public sealed class LateralGroup
        {
            /// <summary>Half the car's width: the car's centre stays this far from the road edge.</summary>
            public float CarHalfWidth = 0.95f;

            /// <summary>How far dragging across the whole screen width moves the target.</summary>
            public float DragMetersPerScreen = 12f;

            /// <summary>How fast a held direction key or stick moves the target, in metres per second.</summary>
            public float AxisSpeed = 8f;

            /// <summary>About how long the car takes to settle on a new target, in seconds.</summary>
            public float SmoothTime = 0.12f;

            public float MaxLateralSpeed = 14f;
        }

        [Serializable]
        public sealed class PoseGroup
        {
            /// <summary>The car turns its nose into a lane change: yaw = atan(sideways speed / speed) × this, up to <see cref="MaxYawDegrees"/>.</summary>
            public float HeadingGain = 0.8f;

            public float MaxYawDegrees = 18f;

            /// <summary>Degrees of body lean (away from the turn) per m/s² of sideways acceleration, up to <see cref="MaxRollDegrees"/>.</summary>
            public float RollPerAcceleration = 0.35f;

            public float MaxRollDegrees = 6f;

            public float RollSharpness = 10f;

            /// <summary>The steering wheel turns this far at the greatest sideways speed; the front wheels turn <see cref="MaxSteerDegrees"/>.</summary>
            public float MaxWheelDegrees = 240f;

            public float MaxSteerDegrees = 22f;

            public float WheelRadius = 0.4f;
        }

        [Serializable]
        public sealed class SignalGroup
        {
            /// <summary>Seconds of one blink, lit for the first half.</summary>
            public float Period = 0.66f;

            /// <summary>A signal always blinks this many times, however short the lane change.</summary>
            public int MinBlinks = 3;

            /// <summary>The car must be this far past a lane line (towards the other lane) to count as having changed lane.</summary>
            public float Hysteresis = 0.3f;
        }

        public SpeedGroup Speed = new SpeedGroup();
        public LateralGroup Lateral = new LateralGroup();
        public PoseGroup Pose = new PoseGroup();
        public SignalGroup Signal = new SignalGroup();

        /// <summary>The furthest the car's centre may be from the road's centre line.</summary>
        public float LateralLimit => RoadSection.HalfWidth - Lateral.CarHalfWidth;

        public void Validate()
        {
            Require(Speed.StartSpeed >= 0f && Speed.CruiseSpeed > 0f && Speed.StartSpeed <= Speed.CruiseSpeed, "Speed: StartSpeed must be in [0, CruiseSpeed] and CruiseSpeed positive.");
            Require(Speed.Acceleration > 0f, "Speed: Acceleration must be positive.");
            Require(Lateral.CarHalfWidth >= 0f && Lateral.CarHalfWidth < RoadSection.HalfWidth, "Lateral: CarHalfWidth must fit on the road.");
            Require(Lateral.DragMetersPerScreen > 0f && Lateral.AxisSpeed > 0f, "Lateral: DragMetersPerScreen and AxisSpeed must be positive.");
            Require(Lateral.SmoothTime > 0f && Lateral.MaxLateralSpeed > 0f, "Lateral: SmoothTime and MaxLateralSpeed must be positive.");
            Require(Pose.HeadingGain >= 0f && Pose.MaxYawDegrees >= 0f && Pose.RollPerAcceleration >= 0f && Pose.MaxRollDegrees >= 0f, "Pose: gains and limits must not be negative.");
            Require(Pose.RollSharpness > 0f && Pose.WheelRadius > 0f, "Pose: RollSharpness and WheelRadius must be positive.");
            Require(Pose.MaxWheelDegrees >= 0f && Pose.MaxSteerDegrees >= 0f, "Pose: wheel and steer angles must not be negative.");
            Require(Signal.Period > 0f && Signal.MinBlinks >= 1 && Signal.Hysteresis >= 0f && Signal.Hysteresis < RoadSection.LineOffset, "Signal: Period positive, MinBlinks at least 1, Hysteresis in [0, lane line offset).");
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
