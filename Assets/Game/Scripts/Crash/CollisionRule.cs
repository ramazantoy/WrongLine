using System;
using System.Collections.Generic;
using WrongLine.Traffic;

namespace WrongLine.Crash
{
    /// <summary>
    /// Whether the player's box meets a traffic car's, in road space: distance along the road and position across it, no physics.
    /// The check walks from where the player was to where it is in steps no longer than a fraction of a metre, so the result does
    /// not depend on the frame rate (a fast frame cannot skip over a car). A first touch that is shallow across the road (the
    /// boxes only just overlap sideways, however they met) is a <see cref="CollisionKind.Sideswipe"/>; any other is a <see cref="CollisionKind.Crash"/>.
    /// A touch that goes on is not reported again, unless it gets deeper than a sideswipe may be. Pure.
    /// </summary>
    public sealed class CollisionRule
    {
        private const int MaxSteps = 64;

        private readonly CrashSettings _settings;
        private readonly HashSet<int> _touching = new HashSet<int>();
        private readonly HashSet<int> _touchingNow = new HashSet<int>();

        public CollisionRule(CrashSettings settings)
        {
            _settings = settings;
        }

        /// <param name="from">The player's box at the last update; the same as <paramref name="to"/> for the first.</param>
        /// <param name="to">The player's box now.</param>
        /// <param name="models">The traffic's models, indexed by <see cref="TrafficCarPlan.Model"/>.</param>
        /// <param name="laneRatio">Metres of lane per metre of distance along the road here (a bend makes it differ from 1); box lengths are divided by it.</param>
        public CollisionResult Update(in PlayerBox from, in PlayerBox to, IReadOnlyList<TrafficCarPlan> cars, IReadOnlyList<TrafficModelSpec> models, double laneRatio)
        {
            CrashSettings.HitboxGroup hit = _settings.Hitbox;
            double ratio = Math.Max(0.1, laneRatio);
            _touchingNow.Clear();
            CollisionResult sideswipe = CollisionResult.None;
            CollisionResult crash = CollisionResult.None;

            for (int i = 0; i < cars.Count; i++)
            {
                TrafficCarPlan car = cars[i];
                TrafficModelSpec model = models[car.Model];
                double reach = (hit.PlayerHalfLength + car.Length * 0.5 * hit.TrafficScale) / ratio;
                double halfWidths = hit.PlayerHalfWidth + model.HalfWidth * hit.TrafficScale;

                double startGap = car.DistanceAt(from.Time) - from.Distance;
                double endGap = car.DistanceAt(to.Time) - to.Distance;
                if ((startGap > reach && endGap > reach) || (startGap < -reach && endGap < -reach))
                {
                    continue; // On the same side of the player at both ends, and clear of it.
                }

                double movement = Math.Abs(endGap - startGap) + Math.Abs(car.LaneXAt(to.Time) - car.LaneXAt(from.Time) - (to.X - from.X));
                int steps = Math.Min(MaxSteps, Math.Max(1, (int)Math.Ceiling(movement / hit.StepMeters)));
                bool wasTouching = _touching.Contains(car.Id);

                for (int step = 1; step <= steps; step++)
                {
                    double f = (double)step / steps;
                    double time = from.Time + (to.Time - from.Time) * f;
                    double gap = car.DistanceAt(time) - (from.Distance + (to.Distance - from.Distance) * f);
                    double across = car.LaneXAt(time) - (from.X + (to.X - from.X) * f);
                    double depthAlong = reach - Math.Abs(gap);
                    double depthAcross = halfWidths - Math.Abs(across);
                    if (depthAlong <= 0.0 || depthAcross <= 0.0)
                    {
                        continue;
                    }

                    _touchingNow.Add(car.Id);
                    float limit = car.Speed < 0.0 ? _settings.Sideswipe.OncomingMaxDepth : _settings.Sideswipe.MaxDepth;
                    bool shallowSide = depthAcross <= limit;
                    if (wasTouching && shallowSide)
                    {
                        break; // The same touch going on.
                    }

                    double span = to.Time - from.Time;
                    double playerSpeed = span > 0.0 ? (to.Distance - from.Distance) / span : 0.0;
                    var result = new CollisionResult(shallowSide ? CollisionKind.Sideswipe : CollisionKind.Crash, car.Id, across >= 0.0 ? 1 : -1, (float)depthAcross, (float)(playerSpeed - car.SpeedAt(time)), gap >= 0.0 ? 1f : -1f);
                    if (result.Kind == CollisionKind.Crash)
                    {
                        if (crash.Kind == CollisionKind.None)
                        {
                            crash = result;
                        }
                    }
                    else if (sideswipe.Kind == CollisionKind.None)
                    {
                        sideswipe = result;
                    }

                    break;
                }
            }

            _touching.Clear();
            foreach (int id in _touchingNow)
            {
                _touching.Add(id);
            }

            return crash.Kind != CollisionKind.None ? crash : sideswipe;
        }
    }
}
