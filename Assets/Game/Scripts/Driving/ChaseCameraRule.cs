using UnityEngine;

namespace WrongLine.Driving
{
    /// <summary>
    /// Where a chase camera goes for a target pose: behind and above it along the road, looking at a point ahead, catching
    /// up exponentially (the same on every frame rate). Pure: the view applies <see cref="Position"/> and <see cref="Rotation"/>.
    /// </summary>
    public sealed class ChaseCameraRule
    {
        private readonly ChaseCameraSettings _settings;
        private bool _started;

        public ChaseCameraRule(ChaseCameraSettings settings)
        {
            settings.Validate();
            _settings = settings;
        }

        public Vector3 Position { get; private set; }

        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        /// <param name="deltaTime">Seconds since the last update; 0 (paused) changes nothing after the first one.</param>
        public void Update(Vector3 targetPosition, Quaternion targetRotation, float deltaTime)
        {
            Vector3 forward = targetRotation * Vector3.forward;
            Vector3 up = Vector3.Slerp(Vector3.up, targetRotation * Vector3.up, _settings.UpBlend).normalized;
            Vector3 desired = targetPosition - forward * _settings.Distance + up * _settings.Height;

            float positionStep = _started ? 1f - Mathf.Exp(-_settings.PositionSharpness * deltaTime) : 1f;
            Position = Vector3.Lerp(Position, desired, positionStep);

            Vector3 lookAt = targetPosition + forward * _settings.LookAhead;
            Quaternion wanted = Quaternion.LookRotation(lookAt - Position, up);
            float rotationStep = _started ? 1f - Mathf.Exp(-_settings.RotationSharpness * deltaTime) : 1f;
            Rotation = Quaternion.Slerp(Rotation, wanted, rotationStep);
            _started = true;
        }

        /// <summary>The scene origin moved: positions in the scene decreased by <paramref name="shift"/>, so the camera does too.</summary>
        public void Shift(Vector3 shift)
        {
            Position -= shift;
        }
    }
}
