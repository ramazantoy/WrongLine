using System;
using UnityEngine;

namespace WrongLine.Crash
{
    /// <summary>
    /// How the parts of a wrecked car fly off: along the way the player was going (a share of its own speed: the car stops, its
    /// parts and body slide on), thrown
    /// up, and away from the car it hit, each with some chance in it and a spin. The chance is a function of the seed and the
    /// part's number, so the same crash always breaks the same way. Pure; the view puts the numbers on rigid bodies.
    /// </summary>
    public sealed class WreckRule
    {
        private readonly CrashSettings.WreckGroup _settings;
        private readonly int _seed;
        private readonly float _speed;
        private readonly float _ahead;
        private readonly float _side;

        /// <param name="seed">The run's seed mixed with the crashed car's id.</param>
        /// <param name="speed">How fast the player's car was going along the road when it hit, in metres per second.</param>
        /// <param name="ahead">+1 when the car hit was ahead of the player, −1 when it was behind.</param>
        /// <param name="side">+1 when it was to the player's right, −1 to the left.</param>
        public WreckRule(CrashSettings.WreckGroup settings, int seed, float speed, float ahead, int side)
        {
            _settings = settings;
            _seed = seed;
            _speed = Mathf.Max(0f, speed);
            _ahead = ahead >= 0f ? 1f : -1f;
            _side = side >= 0 ? 1f : -1f;
        }

        public PartLaunch Launch(int part)
        {
            var random = new System.Random(unchecked(_seed * 7919 + part * 104729 + 17));
            float forward = _ahead * _speed * _settings.SpeedShare * (0.6f + 0.8f * (float)random.NextDouble());

            // Mostly away from the car hit, with some chance either way.
            float across = (-_side * (float)random.NextDouble() + ((float)random.NextDouble() - 0.5f) * 0.6f) * _settings.Spread;
            float up = _settings.UpKick * (0.4f + 0.6f * (float)random.NextDouble());

            Vector3 axis = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f);
            axis = axis.sqrMagnitude > 1e-6f ? axis.normalized : Vector3.up;
            float spin = _settings.SpinDegrees * (float)random.NextDouble();
            return new PartLaunch(new Vector3(across, up, forward), axis * spin);
        }
    }
}
