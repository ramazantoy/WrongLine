using UnityEngine;
using WrongLine.Road;

namespace WrongLine.Car
{
    /// <summary>
    /// The turn signal of a lane change. It starts the moment the player's target lies in another lane than the car, blinks
    /// (lit first) until the car has settled there and has blinked at least the minimum number of times, and flips at once when
    /// the change turns round. Lanes are the road's equal lanes between the lane lines; a line must be crossed by the hysteresis
    /// to count, so a car hovering on a line does not flicker. Pure; time only moves with the delta time (a pause freezes it).
    /// </summary>
    public sealed class TurnSignalRule
    {
        private readonly CarSettings _settings;
        private bool _started;
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

        /// <summary>The lane the car is in: 0 is the leftmost of <see cref="RoadSection.LaneCount"/>.</summary>
        public int CarLane => _carLane;

        public void Update(float x, float targetX, float deltaTime)
        {
            float hysteresis = _settings.Signal.Hysteresis;
            if (!_started)
            {
                // The first update places the car and its target without hysteresis: there is no lane to stay in yet.
                _carLane = LaneAt(x);
                _targetLane = LaneAt(targetX);
                _started = true;
            }
            else
            {
                _carLane = NextLane(_carLane, x, hysteresis);
                _targetLane = NextLane(_targetLane, targetX, hysteresis);
            }

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

        /// <summary>The lane that sideways position <paramref name="x"/> lies in (no hysteresis); positions past the edges count for the outer lanes.</summary>
        public static int LaneAt(float x)
        {
            int lane = Mathf.FloorToInt((x + RoadSection.HalfWidth) / RoadSection.LaneWidth);
            return Mathf.Clamp(lane, 0, RoadSection.LaneCount - 1);
        }

        // Stays in the lane until the position is past one of its lines by the hysteresis, then takes whichever lane it is in.
        private static int NextLane(int lane, float x, float hysteresis)
        {
            float low = -RoadSection.HalfWidth + lane * RoadSection.LaneWidth;
            float high = low + RoadSection.LaneWidth;
            if (lane > 0 && x < low - hysteresis || lane < RoadSection.LaneCount - 1 && x > high + hysteresis)
            {
                return LaneAt(x);
            }

            return lane;
        }
    }
}
