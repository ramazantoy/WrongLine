using System.Collections.Generic;

namespace WrongLine.Traffic
{
    /// <summary>
    /// The traffic as the rest of the game may read it (a later crash feature will): the cars that are out and the run's clock, from
    /// which a car's place on the road is <see cref="TrafficCarPlan.DistanceAt"/>. Read-only.
    /// </summary>
    public interface ITrafficFrame
    {
        IReadOnlyList<TrafficCarPlan> Active { get; }

        /// <summary>Seconds of play so far.</summary>
        double Time { get; }
    }
}
