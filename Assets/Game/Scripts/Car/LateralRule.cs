using UnityEngine;
using WrongLine.Road;

namespace WrongLine.Car
{
    /// <summary>
    /// Where the car is across the road (x, metres right of the centre line). Input moves a target, kept on the road; the car
    /// follows it on a critically damped spring, stepped in small fixed pieces so the result does not depend on the frame rate.
    /// Pure.
    /// </summary>
    public sealed class LateralRule
    {
        private const float MaxStep = 1f / 120f;

        private readonly CarSettings _settings;

        public LateralRule(CarSettings settings)
        {
            settings.Validate();
            _settings = settings;
            X = TargetX = RoadSection.LaneCenterX(settings.Lateral.StartLane);
        }

        public float X { get; private set; }

        /// <summary>Sideways speed, metres per second (positive to the right).</summary>
        public float Velocity { get; private set; }

        public float TargetX { get; private set; }

        /// <param name="dragFraction">Pointer movement since the last update as a fraction of the screen width (right is positive).</param>
        /// <param name="axis">A held direction in [-1, 1].</param>
        public void Update(float dragFraction, float axis, float deltaTime)
        {
            float limit = _settings.LateralLimit;
            CarSettings.LateralGroup group = _settings.Lateral;
            TargetX = Mathf.Clamp(TargetX + dragFraction * group.DragMetersPerScreen + axis * group.AxisSpeed * deltaTime, -limit, limit);

            float omega = 2f / group.SmoothTime;
            float remaining = deltaTime;
            while (remaining > 0f)
            {
                float step = Mathf.Min(remaining, MaxStep);
                remaining -= step;

                float offset = X - TargetX;
                float c2 = Velocity + omega * offset;
                float decay = Mathf.Exp(-omega * step);
                X = TargetX + (offset + c2 * step) * decay;
                Velocity = Mathf.Clamp((Velocity - omega * c2 * step) * decay, -group.MaxLateralSpeed, group.MaxLateralSpeed);
                X = Mathf.Clamp(X, -limit, limit);
            }
        }
    }
}
