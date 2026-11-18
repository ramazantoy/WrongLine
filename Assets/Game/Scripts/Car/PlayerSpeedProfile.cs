using System;

namespace WrongLine.Car
{
    /// <summary>
    /// How fast the player's car goes, how far it has gone and how long it took, as functions of one another. Nothing the player
    /// does changes any of it (there is no gas or brake), so a whole run is known beforehand: that is what lets the traffic be laid
    /// out along the player's path, and the car itself takes its speed and place from here. Three stretches: the car pulls away from
    /// its start speed to the cruise speed (constant acceleration), then keeps gaining speed slowly up to the top speed (constant
    /// gain), then holds it. Pure.
    /// </summary>
    public sealed class PlayerSpeedProfile
    {
        private readonly double _start;
        private readonly double _cruise;
        private readonly double _top;
        private readonly double _acceleration;
        private readonly double _gain;
        private readonly double _rampSeconds;
        private readonly double _rampMeters;
        private readonly double _gainSeconds;
        private readonly double _gainMeters;

        public PlayerSpeedProfile(CarSettings.SpeedGroup speed)
        {
            _start = speed.StartSpeed;
            _cruise = speed.CruiseSpeed;
            _acceleration = speed.Acceleration;
            _rampSeconds = (_cruise - _start) / _acceleration;
            _rampMeters = _start * _rampSeconds + 0.5 * _acceleration * _rampSeconds * _rampSeconds;

            // No gain (or nowhere to gain to): the second stretch is empty and the cruise speed is the top.
            bool gains = speed.GainPerSecond > 0f && speed.MaxSpeed > speed.CruiseSpeed;
            _gain = gains ? speed.GainPerSecond : 0.0;
            _top = gains ? speed.MaxSpeed : _cruise;
            _gainSeconds = gains ? (_top - _cruise) / _gain : 0.0;
            _gainMeters = _cruise * _gainSeconds + 0.5 * _gain * _gainSeconds * _gainSeconds;
        }

        /// <summary>The speed the first stretch ends at.</summary>
        public double Cruise => _cruise;

        /// <summary>The highest speed the run reaches (the cruise speed when the car does not gain speed).</summary>
        public double Top => _top;

        /// <summary>Distance covered after <paramref name="seconds"/>.</summary>
        public double DistanceAt(double seconds)
        {
            if (seconds <= 0.0)
            {
                return 0.0;
            }

            if (seconds < _rampSeconds)
            {
                return _start * seconds + 0.5 * _acceleration * seconds * seconds;
            }

            double since = seconds - _rampSeconds;
            if (since < _gainSeconds)
            {
                return _rampMeters + _cruise * since + 0.5 * _gain * since * since;
            }

            return _rampMeters + _gainMeters + _top * (since - _gainSeconds);
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

            double past = meters - _rampMeters;
            if (past < _gainMeters)
            {
                // cruise·t + g·t²/2 = past
                return _rampSeconds + (-_cruise + Math.Sqrt(_cruise * _cruise + 2.0 * _gain * past)) / _gain;
            }

            return _rampSeconds + _gainSeconds + (past - _gainMeters) / _top;
        }

        /// <summary>The speed after <paramref name="seconds"/>.</summary>
        public double SpeedAtTime(double seconds)
        {
            if (seconds < _rampSeconds)
            {
                return _start + _acceleration * Math.Max(0.0, seconds);
            }

            double since = seconds - _rampSeconds;
            return since < _gainSeconds ? _cruise + _gain * since : _top;
        }

        /// <summary>The speed after <paramref name="meters"/> of road.</summary>
        public double SpeedAtDistance(double meters)
        {
            if (meters < _rampMeters)
            {
                return Math.Sqrt(_start * _start + 2.0 * _acceleration * Math.Max(0.0, meters));
            }

            double past = meters - _rampMeters;
            return past < _gainMeters ? Math.Sqrt(_cruise * _cruise + 2.0 * _gain * past) : _top;
        }
    }
}
