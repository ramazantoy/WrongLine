using System;
using System.Collections.Generic;

namespace WrongLine.Traffic
{
    /// <summary>
    /// The traffic of one run without any scene object: tells the planner how far to plan, and as time passes reports which cars to
    /// put out (they come within spawn range of the player) and which to take back (they are behind the player, or behind the
    /// oldest road piece). Positions follow from the plan and the run's clock, never from adding up steps. Pure.
    /// </summary>
    public sealed class TrafficRun
    {
        private readonly TrafficPlanner _planner;
        private readonly TrafficSettings _settings;
        private readonly List<TrafficCarPlan> _pending = new List<TrafficCarPlan>();
        private readonly List<TrafficCarPlan> _active = new List<TrafficCarPlan>();
        private readonly List<TrafficCarPlan> _spawned = new List<TrafficCarPlan>();
        private readonly List<int> _despawned = new List<int>();
        private int _taken;

        public TrafficRun(TrafficPlanner planner, TrafficSettings settings)
        {
            _planner = planner;
            _settings = settings;
        }

        /// <summary>Seconds of play so far (pause does not count).</summary>
        public double Time { get; private set; }

        public IReadOnlyList<TrafficCarPlan> Active => _active;

        public IReadOnlyList<TrafficCarPlan> Spawned => _spawned;

        /// <summary>Ids of the cars taken back in the last <see cref="Advance"/>.</summary>
        public IReadOnlyList<int> Despawned => _despawned;

        public int PendingCount => _pending.Count;

        /// <param name="playerDistance">Where the player is on the road, in metres.</param>
        /// <param name="oldestRoadDistance">Where the oldest road piece still there begins; a car behind it goes.</param>
        public void Advance(double seconds, double playerDistance, double oldestRoadDistance)
        {
            if (!(seconds >= 0.0))
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), "The run only moves forward.");
            }

            Time += seconds;
            _spawned.Clear();
            _despawned.Clear();

            _planner.PlanTo(playerDistance + _settings.Stream.SpawnAheadMeters * _settings.HorizonRatio + 100.0);
            IReadOnlyList<TrafficCarPlan> planned = _planner.Cars;
            while (_taken < planned.Count)
            {
                _pending.Add(planned[_taken++]);
            }

            double behind = Math.Max(playerDistance - _settings.Stream.DespawnBehindMeters, oldestRoadDistance);
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].DistanceAt(Time) < behind)
                {
                    _despawned.Add(_active[i].Id);
                    _active.RemoveAt(i);
                }
            }

            double ahead = playerDistance + _settings.Stream.SpawnAheadMeters;
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                double distance = _pending[i].DistanceAt(Time);
                if (distance < behind)
                {
                    _pending.RemoveAt(i); // Passed without ever being in range.
                }
                else if (distance <= ahead && _active.Count < _settings.Stream.MaxActive)
                {
                    _spawned.Add(_pending[i]);
                    _active.Add(_pending[i]);
                    _pending.RemoveAt(i);
                }
            }
        }
    }
}
