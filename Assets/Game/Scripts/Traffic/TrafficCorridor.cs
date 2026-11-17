using System;
using System.Collections.Generic;
using WrongLine.Road;

namespace WrongLine.Traffic
{
    /// <summary>
    /// The lane that is kept clear all the way: the player's path is cut into slices, each keeps one lane clear, and the lane may
    /// move by one from one slice to the next. A slice's clear zone reaches half a slice into each neighbour, so where two slices
    /// meet two lanes are clear over a whole slice: room for a lane change. Seeded, built as far as asked. Pure.
    /// </summary>
    public sealed class TrafficCorridor
    {
        private readonly List<int> _lanes = new List<int>();
        private readonly RoadRandom _random;
        private readonly double _start;
        private readonly double _slice;
        private readonly float _stayChance;

        public TrafficCorridor(ulong seed, int startLane, double startSigma, double sliceLength, float stayChance)
        {
            if (startLane < 0 || startLane >= RoadSection.LaneCount)
            {
                throw new ArgumentOutOfRangeException(nameof(startLane));
            }

            _random = new RoadRandom(seed);
            _start = startSigma;
            _slice = sliceLength;
            _stayChance = stayChance;
            _lanes.Add(startLane);
        }

        public double SliceLength => _slice;

        /// <summary>The clear lane of slice <paramref name="index"/>.</summary>
        public int LaneOf(int index)
        {
            while (_lanes.Count <= index)
            {
                _lanes.Add(Next(_lanes[_lanes.Count - 1]));
            }

            return _lanes[index];
        }

        /// <summary>Whether <paramref name="lane" /> must stay clear somewhere in σ from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public bool Reserves(int lane, double from, double to)
        {
            int first = Math.Max(0, (int)Math.Floor((from - _start) / _slice) - 1);
            int last = (int)Math.Floor((to - _start) / _slice) + 1;
            for (int index = first; index <= last; index++)
            {
                double zoneStart = _start + index * _slice - _slice * 0.5;
                double zoneEnd = _start + (index + 1) * _slice + _slice * 0.5;
                if (zoneStart <= to && zoneEnd >= from && LaneOf(index) == lane)
                {
                    return true;
                }
            }

            return false;
        }

        private int Next(int lane)
        {
            // Always draw twice: the sequence must not depend on which branch was taken.
            double stay = _random.NextDouble();
            double side = _random.NextDouble();
            if (stay < _stayChance)
            {
                return lane;
            }

            bool canLeft = lane > 0;
            bool canRight = lane < RoadSection.LaneCount - 1;
            if (canLeft && canRight)
            {
                return side < 0.5 ? lane - 1 : lane + 1;
            }

            return canLeft ? lane - 1 : lane + 1;
        }
    }
}
