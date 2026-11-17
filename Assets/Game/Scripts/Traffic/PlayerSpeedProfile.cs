using System;
using WrongLine.Car;

namespace WrongLine.Traffic
{
    /// <summary>
    /// How far along the road the player's car is at any time, and the other way round. The speed rule pulls away from its start
    /// speed and settles at the cruise speed with nothing the player can change, so the whole run is known beforehand: that is
    /// what lets the traffic be laid out along the player's path. Pure.
    /// </summary>
    public sealed class PlayerSpeedProfile
    {
        private readonly double _start;
        private readonly double _cruise;
        private readonly double _acceleration;
        private readonly double _rampSeconds;
        private readonly double _rampMeters;

        public PlayerSpeedProfile(CarSettings.SpeedGroup speed)
        {
            _start = speed.StartSpeed;
            _cruise = speed.CruiseSpeed;
            _acceleration = speed.Acceleration;
            _rampSeconds = (_cruise - _start) / _acceleration;
            _rampMeters = _start * _rampSeconds + 0.5 * _acceleration * _rampSeconds * _rampSeconds;
        }

        public double Cruise => _cruise;

        /// <summary>Distance covered after <paramref name="seconds"/>.</summary>
        public double DistanceAt(double seconds)
        {
            if (seconds <= 0.0)
            {
                return 0.0;
            }

            return seconds < _rampSeconds
                ? _start * seconds + 0.5 * _acceleration * seconds * seconds
                : _rampMeters + _cruise * (seconds - _rampSeconds);
        }

        /// <summary>Seconds until the car has covered <paramref name="meters"/>.</summary>
        public double TimeAt(double meters)
        {
            if (meters <= 0.0)
            {
                return 0.0;
            }

            if (meters < _rampMeters)
            {
                // start·t + a·t²/2 = meters
                return (-_start + Math.Sqrt(_start * _start + 2.0 * _acceleration * meters)) / _acceleration;
            }

            return _rampSeconds + (meters - _rampMeters) / _cruise;
        }

        /// <summary>The speed after <paramref name="seconds"/>.</summary>
        public double SpeedAtTime(double seconds)
        {
            return seconds < _rampSeconds ? _start + _acceleration * Math.Max(0.0, seconds) : _cruise;
        }

        /// <summary>The speed after <paramref name="meters"/> of road.</summary>
        public double SpeedAtDistance(double meters)
        {
            return meters < _rampMeters ? Math.Sqrt(_start * _start + 2.0 * _acceleration * Math.Max(0.0, meters)) : _cruise;
        }
    }
}
