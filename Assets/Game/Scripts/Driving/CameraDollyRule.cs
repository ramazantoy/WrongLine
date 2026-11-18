using System;
using UnityEngine;

namespace WrongLine.Driving
{
    /// <summary>
    /// Where along the road the camera stands: it slides on the road like a dolly on a track after its subject (the car; after a
    /// crash the wreck). The camera takes up the subject's speed through a filter, so at a steady speed it keeps pace exactly (no lag,
    /// nothing depending on the frame time) while the subject's jolts do not jerk the camera. It stays between
    /// <see cref="ChaseCameraSettings.RangeMin"/> and <see cref="ChaseCameraSettings.RangeMax"/> behind the subject: outside that it
    /// slides, smoothly, to the edge; inside it a weak pull leads it back to <see cref="ChaseCameraSettings.Distance"/>. In a bend
    /// the whole range moves back by up to <see cref="ChaseCameraSettings.BendPullback"/> (the car is easier to keep in the picture
    /// from further away). Pure.
    /// </summary>
    public sealed class CameraDollyRule
    {
        private readonly ChaseCameraSettings _settings;
        private double _lastSubject;
        private double _velocity;
        private bool _started;
        private bool _velocityKnown;

        public CameraDollyRule(ChaseCameraSettings settings)
        {
            settings.Validate();
            _settings = settings;
        }

        /// <summary>The camera's distance along the road, in metres.</summary>
        public double Distance { get; private set; }

        /// <summary>How far behind the subject the camera is, in metres.</summary>
        public double Behind { get; private set; }

        /// <summary>
        /// How much of the bend pull-back the road calls for, from 0 (straight) to 1: the turn between the road's direction at the
        /// camera and at the point it looks at, as a share of <see cref="ChaseCameraSettings.BendPullbackDegrees"/>.
        /// </summary>
        public static float Bend(Quaternion atCamera, Quaternion atLookPoint, ChaseCameraSettings settings)
        {
            Vector3 from = Vector3.ProjectOnPlane(atCamera * Vector3.forward, Vector3.up);
            Vector3 to = Vector3.ProjectOnPlane(atLookPoint * Vector3.forward, Vector3.up);
            if (from.sqrMagnitude < 1e-8f || to.sqrMagnitude < 1e-8f)
            {
                return 0f;
            }

            return Mathf.Clamp01(Vector3.Angle(from, to) / settings.BendPullbackDegrees);
        }

        /// <param name="subjectDistance">Where the subject is along the road.</param>
        /// <param name="deltaTime">0 (paused) leaves the camera where it is.</param>
        /// <param name="bend">0 on a straight, 1 in a sharp bend (see <see cref="Bend"/>): the range moves back by that share of the pull-back.</param>
        public void Update(double subjectDistance, float deltaTime, float bend = 0f)
        {
            double dt = Math.Max(0f, deltaTime);
            double pull = _settings.BendPullback * Math.Min(1f, Math.Max(0f, bend));
            if (!_started)
            {
                Distance = subjectDistance - _settings.Distance - pull;
                _started = true;
            }
            else if (dt > 0.0)
            {
                double speed = (subjectDistance - _lastSubject) / dt;
                if (Math.Abs(speed) <= _settings.MaxTrackSpeed)
                {
                    // The first speed seen is taken as it is; after that the camera's speed eases towards the subject's.
                    _velocity = _velocityKnown ? _velocity + (speed - _velocity) * (1.0 - Math.Exp(-dt / _settings.VelocityTime)) : speed;
                    _velocityKnown = true;
                }

                Distance += _velocity * dt;

                // A weak pull to the nominal distance behind the subject (further back in a bend).
                double nominal = subjectDistance - _settings.Distance - pull;
                Distance += (nominal - Distance) * (1.0 - Math.Exp(-_settings.RestoreSharpness * dt));

                // Out of range: slide to the edge (a share of what is outside per frame, exponentially).
                double behind = subjectDistance - Distance;
                double min = _settings.RangeMin;
                double max = _settings.RangeMax + pull;
                double excess = behind < min ? behind - min : behind > max ? behind - max : 0.0;
                Distance += excess * (1.0 - Math.Exp(-_settings.RangeSharpness * dt));
            }

            _lastSubject = subjectDistance;
            Behind = subjectDistance - Distance;
        }
    }
}
