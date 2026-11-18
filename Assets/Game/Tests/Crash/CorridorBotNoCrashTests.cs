using System;
using NUnit.Framework;
using WrongLine.Car;
using WrongLine.Road;
using WrongLine.Traffic;
using WrongLine.Traffic.Tests;

namespace WrongLine.Crash.Tests
{
    /// <summary>
    /// The traffic planner promises a way through (a lane kept clear, with room to change lane); the collision rule decides what
    /// touches. These meet here: a player who follows the clear lane must never touch anything, and one who ignores it must.
    /// </summary>
    public class CorridorBotNoCrashTests
    {
        private const double Frame = 1.0 / 30.0;

        private static float BotX(TrafficCorridor corridor, double start, double distance)
        {
            double slice = corridor.SliceLength;
            double rel = distance - start;
            if (rel <= 0.0)
            {
                return RoadSection.LaneCenterX(corridor.LaneOf(0));
            }

            int boundary = (int)Math.Round(rel / slice);
            double offset = rel - boundary * slice;
            if (boundary >= 1 && Math.Abs(offset) < slice * 0.25)
            {
                // Over the middle of the slice boundary, where both lanes are clear, move from the old lane to the new one.
                double f = (offset + slice * 0.25) / (slice * 0.5);
                double smooth = f * f * (3.0 - 2.0 * f);
                float from = RoadSection.LaneCenterX(corridor.LaneOf(boundary - 1));
                float to = RoadSection.LaneCenterX(corridor.LaneOf(boundary));
                return (float)(from + (to - from) * smooth);
            }

            int index = offset > 0.0 ? boundary : boundary - 1;
            return RoadSection.LaneCenterX(corridor.LaneOf(Math.Max(0, index)));
        }

        /// <summary>Drives a run for <paramref name="meters"/> and returns the first contact, if any.</summary>
        private static CollisionResult Run(ulong seed, double meters, bool followCorridor)
        {
            var car = new CarSettings();
            TrafficSettings settings = TrafficFixtures.Settings();
            TrafficPlanner planner = TrafficFixtures.Planner(seed, settings, car);
            planner.PlanTo(meters + 300.0);
            var profile = new PlayerSpeedProfile(car.Speed);
            var rule = new CollisionRule(new CrashSettings());
            double start = settings.Layout.ClearStartMeters;
            float fixedX = RoadSection.LaneCenterX(car.Lateral.StartLane);

            double endTime = profile.TimeAt(meters);
            var last = new PlayerBox(0.0, 0.0, fixedX);
            for (double t = Frame; t <= endTime; t += Frame)
            {
                double distance = profile.DistanceAt(t);
                float x = followCorridor ? BotX(planner.Corridor, start, distance) : fixedX;
                var now = new PlayerBox(t, distance, x);
                CollisionResult result = rule.Update(last, now, planner.Cars, TrafficFixtures.Models, 1.0);
                if (result.Kind != CollisionKind.None)
                {
                    return result;
                }

                last = now;
            }

            return CollisionResult.None;
        }

        [Test]
        public void APlayerFollowingTheClearLaneNeverTouchesAnything()
        {
            for (ulong seed = 1; seed <= 20; seed++)
            {
                CollisionResult result = Run(seed, 2000.0, true);

                Assert.AreEqual(CollisionKind.None, result.Kind, $"seed {seed}: touched car {result.CarId} ({result.Kind}, {result.Depth:F2} m deep)");
            }
        }

        [Test]
        public void TheClearLaneStillGetsTheBotThroughOnceTheCarHasReachedItsTopSpeed()
        {
            // 7 km: the car is at its top speed (42 m/s) from about 6 km on, the slices are as short as they get relative to a lane change.
            for (ulong seed = 1; seed <= 4; seed++)
            {
                CollisionResult result = Run(seed, 7000.0, true);

                Assert.AreEqual(CollisionKind.None, result.Kind, $"seed {seed}: touched car {result.CarId} ({result.Kind}, {result.Depth:F2} m deep)");
            }
        }

        [Test]
        public void APlayerStayingInOneLaneRunsIntoSomething()
        {
            // The check itself works: without the clear lane's help the same traffic is hit on most runs.
            int hits = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                if (Run(seed, 2000.0, false).Kind != CollisionKind.None)
                {
                    hits++;
                }
            }

            Assert.GreaterOrEqual(hits, 15);
        }
    }
}
