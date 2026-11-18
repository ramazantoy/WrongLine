namespace WrongLine.Car
{
    /// <summary>
    /// How fast the car goes along the road and how far it has got: both are read off the run's <see cref="PlayerSpeedProfile"/> at
    /// the time the run has lasted (the sum of the frame times), so they are the same numbers the traffic was laid out for. The
    /// profile is built from the settings when the run starts: tuning the speed takes effect at the next run. Pure.
    /// </summary>
    public sealed class SpeedRule
    {
        private readonly PlayerSpeedProfile _profile;

        public SpeedRule(CarSettings settings)
        {
            settings.Validate();
            _profile = new PlayerSpeedProfile(settings.Speed);
            Speed = settings.Speed.StartSpeed;
        }

        /// <summary>Metres per second along the road.</summary>
        public float Speed { get; private set; }

        /// <summary>Metres covered so far.</summary>
        public double Distance { get; private set; }

        /// <summary>Seconds the run has lasted (paused time does not count).</summary>
        public double Time { get; private set; }

        public void Update(float deltaTime)
        {
            Time += deltaTime;
            Speed = (float)_profile.SpeedAtTime(Time);
            Distance = _profile.DistanceAt(Time);
        }
    }
}
