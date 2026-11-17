using System;
using System.Collections.Generic;
using WrongLine.Road;

namespace WrongLine.Traffic
{
    /// <summary>
    /// Lays the traffic out along the player's path, seeded and as far ahead as asked. Everything is placed on σ (where on the road
    /// the player is when it passes a car, see <see cref="TrafficOccupancy"/>), so it does not matter how fast a car goes: each
    /// car is a fixed stretch of σ in its lane. A <see cref="TrafficCorridor"/> keeps one lane clear all the way and lets it move
    /// one lane per slice, with room for the lane change, so a way through always exists. Each lane drives at one speed.
    /// <para>
    /// Some cars change to the neighbouring lane of their direction once. That is decided after the layout around the car is
    /// planned (it is <i>settled</i>): the car joins the other lane at that lane's speed, and the change must be over before the
    /// player reaches it, so in its stretch of σ the car is simply in its new lane. Before that the player is never near it
    /// (the gap only shrinks), and the change must not touch another car (checked along both cars' real paths). Pure.
    /// </para>
    /// </summary>
    public sealed class TrafficPlanner
    {
        private const ulong LaneSalt = 0x7AF1C0DE5EED0001UL;

        private const ulong CorridorSalt = 0xC0221D0123456789UL;

        private const ulong ChangeSalt = 0x5A1D1A9E0C4A9E17UL;

        private readonly IReadOnlyList<TrafficModelSpec> _models;
        private readonly TrafficSettings _settings;
        private readonly PlayerSpeedProfile _player;
        private readonly TrafficCorridor _corridor;
        private readonly LaneState[] _lanes;
        private readonly List<TrafficCarPlan> _all = new List<TrafficCarPlan>();
        private readonly List<TrafficCarPlan> _ready = new List<TrafficCarPlan>();
        private readonly List<int> _unsettled = new List<int>();
        private readonly List<int> _batch = new List<int>();
        private readonly double _totalWeight;
        private readonly ulong _seed;
        private readonly int _paintCount;
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
            _seed = seed;
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

        /// <summary>The cars that are settled (lane changes decided), in the order they were; not sorted by σ.</summary>
        public IReadOnlyList<TrafficCarPlan> Cars => _ready;

        public TrafficCorridor Corridor => _corridor;

        /// <summary>Length of a slice: long enough for the player to change lane (at cruise speed) with some to spare.</summary>
        public static double SliceLength(TrafficSettings settings, PlayerSpeedProfile player)
        {
            return Math.Max(settings.Layout.SliceMeters, player.Cruise * settings.Layout.LaneChangeSeconds * 1.25 + 5.0);
        }

        /// <summary>Settles every car up to σ <paramref name="sigma"/> (what is planned already stays).</summary>
        public void PlanTo(double sigma)
        {
            double target = sigma + _settings.LaneChange.SettleMeters;
            double reached = double.MaxValue;
            for (int lane = 0; lane < _lanes.Length; lane++)
            {
                LaneState state = _lanes[lane];
                while (state.Cursor < target)
                {
                    PlaceNext(lane, state);
                }

                reached = Math.Min(reached, state.Cursor);
            }

            Settle(reached - _settings.LaneChange.SettleMeters);
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
            _unsettled.Add(_all.Count);
            _all.Add(new TrafficCarPlan(_nextId++, model, paint, lane, sigma, half, state.Speed, sigma - state.Speed * time, (float)length));
        }

        /// <summary>
        /// Decides the lane change of every car up to σ <paramref name="boundary"/>, nearest first (so the outcome does not depend on
        /// how the layout was asked for), and hands them over.
        /// </summary>
        private void Settle(double boundary)
        {
            _batch.Clear();
            for (int i = _unsettled.Count - 1; i >= 0; i--)
            {
                if (_all[_unsettled[i]].Sigma <= boundary)
                {
                    _batch.Add(_unsettled[i]);
                    _unsettled.RemoveAt(i);
                }
            }

            _batch.Sort((a, b) =>
            {
                TrafficCarPlan x = _all[a];
                TrafficCarPlan y = _all[b];
                int bySigma = x.Sigma.CompareTo(y.Sigma);
                return bySigma != 0 ? bySigma : x.Lane.CompareTo(y.Lane);
            });

            foreach (int index in _batch)
            {
                TryChangeLane(index);
                _ready.Add(_all[index]);
            }
        }

        private void TryChangeLane(int index)
        {
            TrafficSettings.LaneChangeGroup change = _settings.LaneChange;
            TrafficCarPlan car = _all[index];

            var random = new RoadRandom(_seed ^ ChangeSalt ^ ((ulong)BitConverter.DoubleToInt64Bits(car.Sigma) * 0x9E3779B97F4A7C15UL) ^ (ulong)(car.Lane + 1));
            double chance = random.NextDouble();
            double timeFraction = random.NextDouble();
            double sidePick = random.NextDouble();
            if (chance >= change.Chance)
            {
                return;
            }

            int target = Neighbour(car.Lane, sidePick);
            if (target < 0)
            {
                return;
            }

            double fromSpeed = car.Speed;
            double toSpeed = _lanes[target].Speed;
            double seconds = change.Seconds;

            // When it may start: after it is seen, a few seconds before its old stretch begins, and over before the new one does.
            double latest = _player.TimeAt(car.SigmaStart) - seconds - change.MarginSeconds;
            double earliest = Math.Max(VisibleFrom(car) + 0.5, latest - change.WindowSeconds);
            if (latest - earliest < 0.5)
            {
                return;
            }

            double start = earliest + timeFraction * (latest - earliest);
            if (_player.SpeedAtTime(start) < 1.25 * Math.Max(Math.Abs(fromSpeed), Math.Abs(toSpeed)))
            {
                return; // The player must be well faster than the car, or the gap does not only shrink.
            }

            double end = start + seconds;
            double reached = car.StartDistance + fromSpeed * start + 0.5 * (fromSpeed + toSpeed) * seconds;
            double anchor = reached - toSpeed * end;
            if (!TryMeeting(anchor, toSpeed, out double sigma))
            {
                return;
            }

            double playerSpeed = _player.SpeedAtDistance(sigma);
            if (Math.Abs(playerSpeed - toSpeed) < 0.25 * playerSpeed || Math.Abs(sigma - car.Sigma) > change.MaxShiftMeters)
            {
                return;
            }

            TrafficSettings.LayoutGroup layout = _settings.Layout;
            double half = TrafficOccupancy.HalfLength(car.Length, layout.PlayerLengthMeters, playerSpeed, toSpeed, layout.PadMeters, layout.PadSeconds);
            if (sigma - half < layout.ClearStartMeters || end > _player.TimeAt(sigma - half) - change.MarginSeconds)
            {
                return;
            }

            if (_corridor.Reserves(target, sigma - half, sigma + half))
            {
                return;
            }

            // Its stretch may overlap another car's in the new lane (the lane is then blocked for longer, nothing worse); what must
            // not happen is the cars themselves coming near each other, which is checked along their real paths below.
            var moved = new TrafficCarPlan(car.Id, car.Model, car.Paint, target, sigma, half, toSpeed, anchor, car.Length, car.Lane, fromSpeed, start, seconds);
            if (TouchesAnother(moved, index, car.Lane, target, sigma))
            {
                return;
            }

            _all[index] = moved;
        }

        /// <summary>Whether <paramref name="moved"/>, along its whole change, comes within the conflict gap of any other car in either lane.</summary>
        private bool TouchesAnother(in TrafficCarPlan moved, int index, int laneA, int laneB, double sigma)
        {
            TrafficSettings.LaneChangeGroup change = _settings.LaneChange;
            double from = Math.Max(0.0, moved.ChangeStart - 1.0);
            double to = moved.ChangeEnd + 3.0;
            for (int i = 0; i < _all.Count; i++)
            {
                TrafficCarPlan other = _all[i];
                if (i == index || Math.Abs(other.Sigma - sigma) > 600.0)
                {
                    continue;
                }

                bool inLanes = other.Lane == laneA || other.Lane == laneB || other.FromLane == laneA || other.FromLane == laneB;
                if (!inLanes)
                {
                    continue;
                }

                double room = (moved.Length + other.Length) * 0.5 + change.ConflictGapMeters;
                for (double t = from; t <= to; t += 0.1)
                {
                    bool sameLane = (moved.OccupiesLane(laneA, t) && other.OccupiesLane(laneA, t)) || (moved.OccupiesLane(laneB, t) && other.OccupiesLane(laneB, t));
                    if (sameLane && Math.Abs(moved.DistanceAt(t) - other.DistanceAt(t)) < room)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>The lane next to <paramref name="lane"/> on the same side of the yellow line (one of them if there are two), or −1.</summary>
        private static int Neighbour(int lane, double pick)
        {
            bool oncoming = lane < RoadSection.OncomingLaneCount;
            int first = oncoming ? 0 : RoadSection.OncomingLaneCount;
            int last = oncoming ? RoadSection.OncomingLaneCount - 1 : RoadSection.LaneCount - 1;
            bool left = lane > first;
            bool right = lane < last;
            if (left && right)
            {
                return pick < 0.5 ? lane - 1 : lane + 1;
            }

            return left ? lane - 1 : right ? lane + 1 : -1;
        }

        /// <summary>The first time the car is within the spawn range of the player, in seconds (0 if it starts inside it).</summary>
        private double VisibleFrom(in TrafficCarPlan car)
        {
            double ahead = _settings.Stream.SpawnAheadMeters;
            if (car.StartDistance <= ahead)
            {
                return 0.0;
            }

            double low = 0.0;
            double high = _player.TimeAt(car.Sigma);
            for (int i = 0; i < 50; i++)
            {
                double middle = (low + high) * 0.5;
                if (car.DistanceAt(middle) - _player.DistanceAt(middle) > ahead)
                {
                    low = middle;
                }
                else
                {
                    high = middle;
                }
            }

            return high;
        }

        /// <summary>Where the player meets a car that is at <c>anchor + speed × t</c>, if it does.</summary>
        private bool TryMeeting(double anchor, double speed, out double sigma)
        {
            sigma = 0.0;
            if (!(anchor > 0.0))
            {
                return false;
            }

            double low = 0.0;
            double high = 5000.0;
            if (_player.DistanceAt(high) - (anchor + speed * high) <= 0.0)
            {
                return false;
            }

            for (int i = 0; i < 60; i++)
            {
                double middle = (low + high) * 0.5;
                if (_player.DistanceAt(middle) - (anchor + speed * middle) < 0.0)
                {
                    low = middle;
                }
                else
                {
                    high = middle;
                }
            }

            sigma = _player.DistanceAt(high);
            return true;
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
