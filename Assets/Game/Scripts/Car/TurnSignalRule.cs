using UnityEngine;
using WrongLine.Road;

namespace WrongLine.Car
{
    /// <summary>
    /// The turn signal of a lane change. It starts the moment the player's target lies in another lane than the car, blinks
    /// (lit first) until the car has settled there and has blinked at least the minimum number of times, and flips at once when
    /// the change turns round. Lanes are the three 3 m lanes between the lane lines; a line must be crossed by the hysteresis
    /// to count, so a car hovering on a line does not flicker. Pure; time only moves with the delta time (a pause freezes it).
    /// </summary>
    public sealed class TurnSignalRule
    {
        private readonly CarSettings _settings;
        private int _carLane;
        private int _targetLane;
        private float _time;
        private SignalDirection _direction;

        public TurnSignalRule(CarSettings settings)
        {
            settings.Validate();
            _settings = settings;
        }

        public SignalDirection Direction => _direction;

        /// <summary>Whether the lamp is lit right now (always false with no signal).</summary>
        public bool LampOn => _direction != SignalDirection.None && Mathf.Repeat(_time, _settings.Signal.Period) < _settings.Signal.Period * 0.5f;

        /// <summary>The lane the car is in: -1 left, 0 middle, 1 right.</summary>
        public int CarLane => _carLane;

        public void Update(float x, float targetX, float deltaTime)
        {
            float hysteresis = _settings.Signal.Hysteresis;
            _carLane = NextLane(_carLane, x, hysteresis);
            _targetLane = NextLane(_targetLane, targetX, hysteresis);

            if (_targetLane != _carLane)
            {
                SignalDirection wanted = _targetLane > _carLane ? SignalDirection.Right : SignalDirection.Left;
                if (wanted != _direction)
                {
                    _direction = wanted;
                    _time = 0f;
                }
            }

            if (_direction == SignalDirection.None)
            {
                return;
            }

            _time += deltaTime;
            bool settled = _targetLane == _carLane;
            if (settled && _time >= _settings.Signal.MinBlinks * _settings.Signal.Period)
            {
                _direction = SignalDirection.None;
                _time = 0f;
            }
        }

        private static int NextLane(int lane, float x, float hysteresis)
        {
            float line = RoadSection.LineOffset;
            switch (lane)
            {
                case -1:
                    return x > -line + hysteresis ? NextLane(0, x, hysteresis) : -1;
                case 1:
                    return x < line - hysteresis ? NextLane(0, x, hysteresis) : 1;
                default:
                    if (x < -line - hysteresis)
                    {
                        return -1;
                    }

                    return x > line + hysteresis ? 1 : 0;
            }
        }
    }
}
