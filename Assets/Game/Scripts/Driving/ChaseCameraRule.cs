using UnityEngine;

namespace WrongLine.Driving
{
    /// <summary>
    /// Where a chase camera goes: on the road's own centre line a fixed distance behind the car and above it, looking at the
    /// road a fixed distance ahead. Along the road the camera is exact, never "catching up", so the car sits at the same place
    /// in the picture whatever the frame times do. Only the sideways position is smoothed (the player's own movement), and that
    /// catches up exponentially. Pure: the view applies <see cref="Position"/>, <see cref="Rotation"/> and <see cref="Fov"/>.
    /// </summary>
    public sealed class ChaseCameraRule
    {
        private readonly ChaseCameraSettings _settings;
        private float _lateral;
        private float _lookLateral;
        private bool _started;

        public ChaseCameraRule(ChaseCameraSettings settings)
        {
            settings.Validate();
            _settings = settings;
        }

        public Vector3 Position { get; private set; }

        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        /// <summary>The vertical field of view in degrees.</summary>
        public float Fov { get; private set; } = 60f;

        /// <param name="deltaTime">Seconds since the last update; 0 (paused) leaves the sideways position where it is.</param>
        public void Update(in ChaseTarget target, float deltaTime)
        {
            float wantedLateral = target.CarX * _settings.FollowX;
            float wantedLook = target.CarX * _settings.LookFollowX;
            if (_started)
            {
                float step = 1f - Mathf.Exp(-_settings.LateralSharpness * deltaTime);
                _lateral = Mathf.Lerp(_lateral, wantedLateral, step);
                _lookLateral = Mathf.Lerp(_lookLateral, wantedLook, step);
            }
            else
            {
                _lateral = wantedLateral;
                _lookLateral = wantedLook;
                _started = true;
            }

            Vector3 roadUp = target.RearRotation * Vector3.up;
            Vector3 up = Vector3.Slerp(Vector3.up, roadUp, _settings.UpBlend).normalized;
            Position = target.RearPosition + target.RearRotation * Vector3.right * _lateral + up * _settings.Height;

            Vector3 lookAt = target.AheadPosition + target.AheadRotation * Vector3.right * _lookLateral;
            Vector3 look = KeepCarInPicture(lookAt - Position, target.CarPosition - Position, up, _settings.MaxCarYawDegrees, _settings.MaxCarPitchDegrees);
            Rotation = Quaternion.LookRotation(look, up);

            float speedShare = Mathf.InverseLerp(_settings.FovMinSpeed, _settings.FovMaxSpeed, target.Speed);
            Fov = Mathf.Lerp(_settings.BaseFov, _settings.MaxFov, speedShare);
        }

        /// <summary>
        /// Turns the look direction until it is within <paramref name="maxYaw"/> sideways and <paramref name="maxPitch"/> up or
        /// down of the direction to the car. Looking ahead (into a bend, over a crest) is the point, but never so far that the
        /// car leaves the picture.
        /// </summary>
        private static Vector3 KeepCarInPicture(Vector3 look, Vector3 toCar, Vector3 up, float maxYaw, float maxPitch)
        {
            Vector3 lookDirection = look.normalized;
            Vector3 carDirection = toCar.normalized;

            // Sideways first: turn about up, which leaves the height of the direction alone.
            Vector3 lookFlat = Vector3.ProjectOnPlane(lookDirection, up);
            Vector3 carFlat = Vector3.ProjectOnPlane(carDirection, up);
            if (lookFlat.sqrMagnitude > 1e-8f && carFlat.sqrMagnitude > 1e-8f)
            {
                float yaw = Vector3.SignedAngle(carFlat, lookFlat, up);
                float limited = Mathf.Clamp(yaw, -maxYaw, maxYaw);
                if (!Mathf.Approximately(limited, yaw))
                {
                    lookDirection = Quaternion.AngleAxis(limited - yaw, up) * lookDirection;
                }
            }

            float lookElevation = Elevation(lookDirection, up);
            float carElevation = Elevation(carDirection, up);
            float wanted = Mathf.Clamp(lookElevation, carElevation - maxPitch, carElevation + maxPitch);
            Vector3 flat = Vector3.ProjectOnPlane(lookDirection, up);
            if (Mathf.Approximately(wanted, lookElevation) || flat.sqrMagnitude < 1e-8f)
            {
                return lookDirection;
            }

            float radians = wanted * Mathf.Deg2Rad;
            return flat.normalized * Mathf.Cos(radians) + up * Mathf.Sin(radians);
        }

        private static float Elevation(Vector3 direction, Vector3 up)
        {
            return Mathf.Asin(Mathf.Clamp(Vector3.Dot(direction, up), -1f, 1f)) * Mathf.Rad2Deg;
        }
    }
}
