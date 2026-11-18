using System;
using UnityEngine;

namespace WrongLine.Car
{
    /// <summary>
    /// How the car looks while it moves sideways, from its sideways speed and its speed along the road: the nose turns into the
    /// lane change, the body leans away from it, the steering wheel and the front wheels turn, the wheels spin. Pure; the view
    /// applies the angles. Angles are Unity's: yaw about up (positive is right), roll about forward (positive leans right).
    /// </summary>
    public sealed class CarPoseRule
    {
        private readonly CarSettings _settings;
        private float _lastVelocity;
        private bool _started;
        private float _roll;
        private float _kickYaw;
        private float _kickRoll;
        private float _kickSeconds = 1f;
        private float _kickLeft;

        public CarPoseRule(CarSettings settings)
        {
            settings.Validate();
            _settings = settings;
        }

        public float YawDegrees { get; private set; }

        public float RollDegrees { get; private set; }

        /// <summary>The steering wheel's turn; positive is clockwise for the driver, a turn to the right.</summary>
        public float SteeringWheelDegrees { get; private set; }

        public float FrontWheelSteerDegrees { get; private set; }

        /// <summary>How far the wheels have rolled, wrapped to [0, 360).</summary>
        public float WheelSpinDegrees { get; private set; }

        /// <summary>
        /// A sideswipe's kick: the nose and the body jerk towards <paramref name="direction"/> (+1 right, −1 left, the way the car is
        /// pushed) by up to these angles and settle over <paramref name="seconds"/>. Only the look of the car; the view applies it.
        /// </summary>
        public void Kick(int direction, float yawDegrees, float rollDegrees, float seconds)
        {
            float sign = direction >= 0 ? 1f : -1f;
            _kickYaw = sign * yawDegrees;
            _kickRoll = -sign * rollDegrees;
            _kickSeconds = Mathf.Max(seconds, 0.01f);
            _kickLeft = _kickSeconds;
        }

        public void Update(float sidewaysVelocity, float speed, float deltaTime)
        {
            CarSettings.PoseGroup pose = _settings.Pose;
            float maxSpeed = _settings.Lateral.MaxLateralSpeed;

            float heading = Mathf.Atan2(sidewaysVelocity, Mathf.Max(speed, 1f)) * Mathf.Rad2Deg * pose.HeadingGain;
            YawDegrees = Mathf.Clamp(heading, -pose.MaxYawDegrees, pose.MaxYawDegrees);

            float steer = Mathf.Clamp(sidewaysVelocity / maxSpeed, -1f, 1f);
            SteeringWheelDegrees = steer * pose.MaxWheelDegrees;
            FrontWheelSteerDegrees = steer * pose.MaxSteerDegrees;

            if (deltaTime > 0f)
            {
                float acceleration = _started ? (sidewaysVelocity - _lastVelocity) / deltaTime : 0f;
                float wanted = Mathf.Clamp(-acceleration * pose.RollPerAcceleration, -pose.MaxRollDegrees, pose.MaxRollDegrees);
                _roll = Mathf.Lerp(_roll, wanted, 1f - Mathf.Exp(-pose.RollSharpness * deltaTime));
                WheelSpinDegrees = Mod(WheelSpinDegrees + speed / pose.WheelRadius * Mathf.Rad2Deg * deltaTime, 360f);
                _lastVelocity = sidewaysVelocity;
                _started = true;
                _kickLeft = Mathf.Max(0f, _kickLeft - deltaTime);
            }

            // The kick fades out smoothly (squared), on top of the angles above.
            float fade = _kickLeft / _kickSeconds;
            fade *= fade;
            YawDegrees += _kickYaw * fade;
            RollDegrees = _roll + _kickRoll * fade;
        }

        private static float Mod(float value, float modulus)
        {
            return value - (float)Math.Floor(value / modulus) * modulus;
        }
    }
}
