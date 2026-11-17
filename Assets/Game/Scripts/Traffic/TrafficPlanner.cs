using System;
using System.Collections.Generic;
using WrongLine.Road;

namespace WrongLine.Traffic
{
    /// <summary>
    /// Lays the traffic out along the player's path, seeded and as far ahead as asked. Everything is placed on σ (where on the road
    /// the player is when it passes a car, see <see cref="TrafficOccupancy"/>), so it does not matter how fast a car goes: each
    /// car is a fixed stretch of σ in its lane. A <see cref="TrafficCorridor"/> keeps one lane clear all the way and lets it move
    /// one lane per slice, with room for the lane change, so a way through always exists. Each lane drives at one speed (no car
    /// ever catches another). Pure.
    /// </summary>
    public sealed class TrafficPlanner
    {
        /// <summary>A car that blocks <c>[σ − half, σ + half]</c> of its lane may start no earlier than the clear start.</summary>
        private const ulong LaneSalt = 0x7AF1C0DE5EED0001UL;

        private const ulong CorridorSalt = 0xC0221D0123456789UL;

        private readonly IReadOnlyList<TrafficModelSpec> _models;
        private readonly TrafficSettings _settings;
        private readonly PlayerSpeedProfile _player;
        private readonly TrafficCorridor _corridor;
        private readonly LaneState[] _lanes;
        private readonly List<TrafficCarPlan> _cars = new List<TrafficCarPlan>();
        private readonly double _totalWeight;
        private int _nextId;

        public TrafficPlanner(IReadOnlyList<TrafficModelSpec> models, TrafficSettings settings, PlayerSpeedProfile player, ulong seed, int startLane, int paintCount)
        {
            if (models.Count == 0)
            {
                throw new ArgumentException("There is no traffic model.", nameof(models));
            }

            if (paintCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(paintCount));
            }

            _models = models;
            _settings = settings;
            _player = player;
            _paintCount = paintCount;
            foreach (TrafficModelSpec model in models)
            {
                _totalWeight += model.Weight;
            }

            double slice = SliceLength(settings, player);
            _corridor = new TrafficCorridor(seed ^ CorridorSalt, startLane, settings.Layout.ClearStartMeters, slice, settings.Layout.CorridorStayChance);

            _lanes = new LaneState[RoadSection.LaneCount];
            for (int lane = 0; lane < _lanes.Length; lane++)
            {
                var random = new RoadRandom(seed ^ LaneSalt ^ ((ulong)(lane + 1) * 0x9E3779B97F4A7C15UL));
                double fraction = settings.Layout.LaneSpeedMin + (settings.Layout.LaneSpeedMax - settings.Layout.LaneSpeedMin) * random.NextDouble();
                double speed = fraction * player.Cruise;
                _lanes[lane] = new LaneState(random, lane < RoadSection.OncomingLaneCount ? -speed : speed, settings.Layout.ClearStartMeters);
            }
        }

        private readonly int _paintCount;

        /// <summary>The planned cars, in the order they were made (not sorted by σ).</summary>
        public IReadOnlyList<TrafficCarPlan> Cars => _cars;

        public TrafficCorridor Corridor => _corridor;

        /// <summary>Length of a slice: long enough for the player to change lane (at cruise speed) with some to spare.</summary>
        public static double SliceLength(TrafficSettings settings, PlayerSpeedProfile player)
        {
            return Math.Max(settings.Layout.SliceMeters, player.Cruise * settings.Layout.LaneChangeSeconds * 1.25 + 5.0);
        }

        /// <summary>Plans every lane up to σ <paramref name="sigma"/> (what is planned already stays).</summary>
        public void PlanTo(double sigma)
        {
            for (int lane = 0; lane < _lanes.Length; lane++)
            {
                LaneState state = _lanes[lane];
                while (state.Cursor < sigma)
                {
                    PlaceNext(lane, state);
                }
            }
        }

        private void PlaceNext(int lane, LaneState state)
        {
            TrafficSettings.LayoutGroup layout = _settings.Layout;

            // Draw in a fixed order: the numbers a lane uses must not depend on what got rejected.
            double gap = layout.MinGapMeters + layout.GapRangeMeters * state.Random.NextDouble();
            double pick = state.Random.NextDouble() * _totalWeight;
            int paint = (int)(state.Random.NextDouble() * _paintCount);

            int model = 0;
            double sum = 0.0;
            for (int i = 0; i < _models.Count; i++)
            {
                sum += _models[i].Weight;
                if (pick < sum)
                {
                    model = i;
                    break;
                }
            }

            double length = _models[model].Length;
            double centerGuess = state.Cursor + gap;
            double playerSpeed = _player.SpeedAtDistance(centerGuess);
            if (Math.Abs(playerSpeed - state.Speed) < 0.2 * playerSpeed)
            {
                // The player is still too slow to pass this lane's cars (the start of a run): leave a stretch.
                state.Cursor = centerGuess;
                return;
            }

            double half = TrafficOccupancy.HalfLength(length, layout.PlayerLengthMeters, playerSpeed, state.Speed, layout.PadMeters, layout.PadSeconds);
            double sigma = Math.Max(centerGuess + half, layout.ClearStartMeters + half);
            double start = sigma - half;
            double end = sigma + half;
            state.Cursor = end;

            if (_corridor.Reserves(lane, start, end))
            {
                return;
            }

            double time = _player.TimeAt(sigma);
            _cars.Add(new TrafficCarPlan(_nextId++, model, paint, lane, sigma, half, state.Speed, sigma - state.Speed * time, (float)length));
        }

        private sealed class LaneState
        {
            public readonly RoadRandom Random;
            public readonly double Speed;
            public double Cursor;

            public LaneState(RoadRandom random, double speed, double cursor)
            {
                Random = random;
                Speed = speed;
                Cursor = cursor;
            }
        }
    }
}
