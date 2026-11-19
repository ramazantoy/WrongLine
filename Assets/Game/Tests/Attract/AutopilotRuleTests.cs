using System;
using System.Collections.Generic;
using NUnit.Framework;
using WrongLine.Car;
using WrongLine.Crash;
using WrongLine.Road;
using WrongLine.Traffic;
using WrongLine.Traffic.Tests;

namespace WrongLine.Attract.Tests
{
    public class AutopilotRuleTests
    {
        private const int Seeds = 40;
        private const double DriveMeters = 3000.0;

        [Test]
        public void TheCarKeepsTheStartLaneUntilTheFirstChangeIsDue()
        {
            TrafficPlanner planner = TrafficFixtures.Planner(3UL);
            TrafficCorridor corridor = planner.Corridor;
            var rule = new AutopilotRule(new AutopilotSettings());

            for (double sigma = -50.0; sigma < corridor.Start; sigma += 5.0)
            {
                Assert.AreEqual(corridor.LaneOf(0), rule.LaneAt(corridor, sigma), $"σ {sigma}");
            }
        }

        [Test]
        public void TheCarTakesTheNextSlicesLaneJustAfterTheTwoLanesStartToOverlap()
        {
            TrafficPlanner planner = TrafficFixtures.Planner(3UL);
            TrafficCorridor corridor = planner.Corridor;
            var settings = new AutopilotSettings();
            var rule = new AutopilotRule(settings);

            for (int k = 1; k <= 30; k++)
            {
                double due = corridor.Start + (k - 0.5 + settings.ChangeFraction) * corridor.SliceLength;
                Assert.AreEqual(corridor.LaneOf(k - 1), rule.LaneAt(corridor, due - 0.5), $"slice {k}, just before");
                Assert.AreEqual(corridor.LaneOf(k), rule.LaneAt(corridor, due + 0.5), $"slice {k}, just after");
            }
        }

        [Test]
        public void TheLaneTheCarIsToBeInIsNeverBlockedByACar()
        {
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                TrafficPlanner planner = TrafficFixtures.Planner(seed);
                planner.PlanTo(DriveMeters + 500.0);
                var rule = new AutopilotRule(new AutopilotSettings());

                // Along each car's own stretch of σ: the lane the car is to be in there is not the car's.
                foreach (TrafficCarPlan car in planner.Cars)
                {
                    for (double sigma = car.SigmaStart; sigma <= car.SigmaEnd; sigma += 0.5)
                    {
                        Assert.AreNotEqual(car.Lane, rule.LaneAt(planner.Corridor, sigma), $"seed {seed}: car {car.Id} blocks lane {car.Lane} at σ {sigma}");
                    }
                }
            }
        }

        [Test]
        public void TheAxisPointsTowardsTheLaneAndEasesIn()
        {
            var rule = new AutopilotRule(new AutopilotSettings { AxisGain = 2f });

            Assert.AreEqual(1f, rule.Axis(4.5f, -1.5f), 1e-6f, "Far to the right: full.");
            Assert.AreEqual(-1f, rule.Axis(-4.5f, 1.5f), 1e-6f, "Far to the left: full.");
            Assert.AreEqual(0.4f, rule.Axis(1.7f, 1.5f), 1e-6f, "Close: in proportion.");
            Assert.AreEqual(0f, rule.Axis(1.5f, 1.5f), 1e-6f, "There: nothing.");
        }

        [Test]
        public void ASettingOutOfRangeIsRefused()
        {
            Assert.Throws<ArgumentException>(() => new AutopilotRule(new AutopilotSettings { ChangeFraction = 0.9f }));
            Assert.Throws<ArgumentException>(() => new AutopilotRule(new AutopilotSettings { AxisGain = 0f }));
        }

        [TestCase(1.0 / 60.0)]
        [TestCase(1.0 / 20.0)]
        public void DrivingWithTheAutopilotNeverTouchesACarAtTheGameSpeed(double step)
        {
            Drive(new CarSettings(), step);
        }

        [TestCase(1.0 / 60.0)]
        [TestCase(1.0 / 20.0)]
        public void DrivingWithTheAutopilotNeverTouchesACarAtASteadySlowerSpeed(double step)
        {
            var car = new CarSettings();
            car.Speed.StartSpeed = 20f;
            car.Speed.CruiseSpeed = 24f;
            car.Speed.GainPerSecond = 0f;
            car.Speed.MaxSpeed = 24f;
            Drive(car, step);
        }

        /// <summary>Drives the autopilot's car through the seeds' traffic the way the game does and fails on the first touch (a sideswipe counts).</summary>
        private static void Drive(CarSettings car, double step)
        {
            TrafficSettings traffic = TrafficFixtures.Settings();
            var crash = new CrashSettings();
            int changes = 0;

            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var profile = new PlayerSpeedProfile(car.Speed);
                TrafficPlanner planner = TrafficFixtures.Planner(seed, traffic, car);
                var rule = new AutopilotRule(new AutopilotSettings());
                var lateral = new LateralRule(car);
                var collisions = new CollisionRule(crash);
                var near = new List<TrafficCarPlan>();

                double time = 0.0;
                var previous = new PlayerBox(0.0, 0.0, lateral.X);
                int lane = RoadSection.LaneCount;
                while (profile.DistanceAt(time) < DriveMeters)
                {
                    time += step;
                    double distance = profile.DistanceAt(time);
                    planner.PlanTo(distance + 800.0);

                    int wanted = rule.LaneAt(planner.Corridor, distance);
                    if (lane != RoadSection.LaneCount && wanted != lane)
                    {
                        changes++;
                    }

                    lane = wanted;
                    lateral.Update(0f, rule.Axis(RoadSection.LaneCenterX(wanted), lateral.TargetX), (float)step);

                    near.Clear();
                    foreach (TrafficCarPlan plan in planner.Cars)
                    {
                        if (Math.Abs(plan.Sigma - distance) < 200.0)
                        {
                            near.Add(plan);
                        }
                    }

                    var box = new PlayerBox(time, distance, lateral.X);
                    CollisionResult result = collisions.Update(previous, box, near, TrafficFixtures.Models, 1.0);
                    Assert.AreEqual(CollisionKind.None, result.Kind, $"seed {seed}: touched car {result.CarId} at {distance:F0} m (x {lateral.X:F2}, wanted lane {wanted})");
                    previous = box;
                }
            }

            Assert.Greater(changes, Seeds * 10, "The drive changed lanes now and then (or it proves nothing).");
        }
    }
}
