using UnityEngine;

namespace WrongLine.Car
{
    /// <summary>How fast the car goes along the road: it pulls away from its start speed and settles at the cruise speed. Pure.</summary>
    public sealed class SpeedRule
    {
        private readonly CarSettings _settings;

        public SpeedRule(CarSettings settings)
        {
            settings.Validate();
            _settings = settings;
            Speed = settings.Speed.StartSpeed;
        }

        /// <summary>Metres per second along the road.</summary>
        public float Speed { get; private set; }

        public void Update(float deltaTime)
        {
            Speed = Mathf.MoveTowards(Speed, _settings.Speed.CruiseSpeed, _settings.Speed.Acceleration * deltaTime);
        }
    }
}
