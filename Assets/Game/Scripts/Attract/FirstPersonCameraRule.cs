using UnityEngine;

namespace WrongLine.Attract
{
    /// <summary>
    /// Where the camera is when it sits in the driver's seat of the car: the car's body (already on the road, yawed and rolled as its
    /// pose rule says) carries it, so a lane change tilts and turns the picture with the car. Pure.
    /// </summary>
    public sealed class FirstPersonCameraRule
    {
        private readonly FirstPersonCameraSettings _settings;

        public FirstPersonCameraRule(FirstPersonCameraSettings settings)
        {
            settings.Validate();
            _settings = settings;
        }

        public Vector3 Position(Vector3 bodyPosition, Quaternion bodyRotation)
        {
            return bodyPosition + bodyRotation * new Vector3(0f, _settings.EyeHeight, _settings.EyeForward);
        }

        public Quaternion Rotation(Quaternion bodyRotation)
        {
            return bodyRotation * Quaternion.Euler(_settings.PitchDownDegrees, 0f, 0f);
        }

        /// <summary>The field of view for <paramref name="speed"/> (metres per second): wider the faster the car goes.</summary>
        public float Fov(float speed)
        {
            float share = Mathf.InverseLerp(_settings.FovMinSpeed, _settings.FovMaxSpeed, speed);
            return Mathf.Lerp(_settings.BaseFov, _settings.MaxFov, share);
        }
    }
}
