using System;
using System.Collections.Generic;
using WrongLine.Road;

namespace WrongLine.Traffic
{
    /// <summary>
    /// An independent check that a planned traffic can be driven through: it knows nothing of the corridor the planner kept
    /// clear. It lays the cars' blocked stretches of σ on a one-metre grid (rounded outwards) and asks, lane by lane, whether the
    /// player can get from the start lane to the end by staying in a free lane or changing to a neighbour (both lanes free the
    /// whole length of the change). Used by tests and measurements, not at run time. Pure.
    /// </summary>
    public static class TrafficPassability
    {
        public readonly struct Result
        {
            public readonly bool Passable;

            /// <summary>The furthest σ the player can reach (the end when passable), in metres.</summary>
            public readonly int ReachedMeters;

            public Result(bool passable, int reachedMeters)
            {
                Passable = passable;
                ReachedMeters = reachedMeters;
            }
        }

        /// <param name="changeMeters">Length of road a lane change takes (at the greatest speed).</param>
        public static Result Check(IReadOnlyList<TrafficCarPlan> cars, int startLane, int fromMeters, int toMeters, int changeMeters)
        {
            int cells = toMeters - fromMeters + 1;
            int lanes = RoadSection.LaneCount;
            var blocked = new bool[lanes][];
            for (int lane = 0; lane < lanes; lane++)
            {
                blocked[lane] = new bool[cells + changeMeters + 2];
            }

            foreach (TrafficCarPlan car in cars)
            {
                int lo = Math.Max(0, (int)Math.Floor(car.SigmaStart) - fromMeters);
                int hi = Math.Min(blocked[car.Lane].Length - 1, (int)Math.Ceiling(car.SigmaEnd) - fromMeters);
                for (int cell = lo; cell <= hi; cell++)
                {
                    blocked[car.Lane][cell] = true;
                }
            }

            // freeRun[lane][i]: how many free cells in a row start at i.
            var freeRun = new int[lanes][];
            for (int lane = 0; lane < lanes; lane++)
            {
                bool[] row = blocked[lane];
                freeRun[lane] = new int[row.Length + 1];
                for (int i = row.Length - 1; i >= 0; i--)
                {
                    freeRun[lane][i] = row[i] ? 0 : freeRun[lane][i + 1] + 1;
                }
            }

            var reach = new bool[lanes][];
            for (int lane = 0; lane < lanes; lane++)
            {
                reach[lane] = new bool[blocked[lane].Length];
            }

            if (freeRun[startLane][0] == 0)
            {
                return new Result(false, 0);
            }

            reach[startLane][0] = true;
            int furthest = 0;
            for (int i = 0; i < cells; i++)
            {
                for (int lane = 0; lane < lanes; lane++)
                {
                    if (!reach[lane][i])
                    {
                        continue;
                    }

                    furthest = Math.Max(furthest, i);
                    if (!blocked[lane][i + 1])
                    {
                        reach[lane][i + 1] = true;
                    }

                    for (int side = -1; side <= 1; side += 2)
                    {
                        int other = lane + side;
                        if (other < 0 || other >= lanes)
                        {
                            continue;
                        }

                        if (freeRun[lane][i] > changeMeters && freeRun[other][i] > changeMeters)
                        {
                            reach[other][i + changeMeters] = true;
                        }
                    }
                }
            }

            for (int lane = 0; lane < lanes; lane++)
            {
                if (reach[lane][cells - 1] || reach[lane][cells])
                {
                    return new Result(true, toMeters);
                }
            }

            return new Result(false, fromMeters + furthest);
        }
    }
}
