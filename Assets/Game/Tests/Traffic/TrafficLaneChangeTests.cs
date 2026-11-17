using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WrongLine.Car;
using WrongLine.Road;

namespace WrongLine.Traffic.Tests
{
    public class TrafficLaneChangeTests
    {
        private static TrafficCarPlan Changer(int from, int to, double fromSpeed, double toSpeed, double start, double seconds, double anchor = 100.0)
        {
            return new TrafficCarPlan(1, 0, 0, to, 1000.0, 10.0, toSpeed, anchor, 4f, from, fromSpeed, start, seconds);
        }

        private static List<TrafficCarPlan> Changers(IReadOnlyList<TrafficCarPlan> cars)
        {
            return cars.Where(car => car.HasChange).ToList();
        }

        private static TrafficSettings Settings(float chance)
        {
            TrafficSettings settings = TrafficFixtures.Settings();
            settings.LaneChange.Chance = chance;
            return settings;
        }

        [Test]
        public void TheDistanceIsContinuous_AndItsDerivativeIsTheSpeed_AcrossAChange()
        {
            TrafficCarPlan car = Changer(2, 3, 12.0, 21.0, 10.0, 1.5);

            foreach (double edge in new[] { 10.0, 11.5 })
            {
                Assert.AreEqual(car.DistanceAt(edge - 1e-6), car.DistanceAt(edge + 1e-6), 1e-4, $"jump at {edge}");
            }

            for (double t = 8.0; t < 14.0; t += 0.05)
            {
                if (System.Math.Abs(t - 10.0) < 0.01 || System.Math.Abs(t - 11.5) < 0.01)
                {
                    continue; // The speed bends there: a difference across the corner is not its derivative.
                }

                double measured = (car.DistanceAt(t + 0.001) - car.DistanceAt(t - 0.001)) / 0.002;
                Assert.AreEqual(car.SpeedAt(t), measured, 1e-3, $"speed at {t}");
            }

            Assert.AreEqual(12.0, car.SpeedAt(9.0));
            Assert.AreEqual(21.0, car.SpeedAt(12.0));
            Assert.AreEqual(16.5, car.SpeedAt(10.75), 1e-9);
        }

        [Test]
        public void ACarThatDoesNotChange_MovesInAStraightLine()
        {
            var car = new TrafficCarPlan(1, 0, 0, 2, 500.0, 8.0, 15.0, 50.0, 4f);

            Assert.IsFalse(car.HasChange);
            Assert.AreEqual(50.0 + 15.0 * 7.0, car.DistanceAt(7.0), 1e-9);
            Assert.AreEqual(RoadSection.LaneCenterX(2), car.LaneXAt(3.0));
            Assert.AreEqual(0.0, car.LateralSpeedAt(3.0));
            Assert.AreEqual(0, car.SignalSideAt(3.0, 1.0, 1.0));
        }

        [Test]
        public void TheLaneMovesFromOneMiddleToTheOther_AndTheSidewaysSpeedAddsUpToTheDistance()
        {
            TrafficCarPlan car = Changer(2, 3, 15.0, 15.0, 10.0, 1.4);

            Assert.AreEqual(RoadSection.LaneCenterX(2), car.LaneXAt(9.0), 1e-9);
            Assert.AreEqual(RoadSection.LaneCenterX(3), car.LaneXAt(12.0), 1e-9);
            Assert.AreEqual((RoadSection.LaneCenterX(2) + RoadSection.LaneCenterX(3)) * 0.5, car.LaneXAt(10.7), 1e-9);

            double sideways = 0.0;
            for (double t = 10.0; t < 11.4; t += 0.001)
            {
                sideways += car.LateralSpeedAt(t) * 0.001;
            }

            Assert.AreEqual(RoadSection.LaneWidth, sideways, 0.01);
        }

        [Test]
        public void ACarChangingLaneIsInBothLanesUntilItIsOver()
        {
            TrafficCarPlan car = Changer(0, 1, -10.0, -14.0, 10.0, 1.4);

            Assert.IsTrue(car.OccupiesLane(0, 5.0));
            Assert.IsFalse(car.OccupiesLane(1, 5.0));
            Assert.IsTrue(car.OccupiesLane(0, 10.5) && car.OccupiesLane(1, 10.5));
            Assert.IsTrue(car.OccupiesLane(1, 12.0));
            Assert.IsFalse(car.OccupiesLane(0, 12.0));
        }

        [Test]
        public void TheSignalPointsTheWayOfTheChange_StartsEarlyEndsLate_AndBlinks()
        {
            TrafficCarPlan right = Changer(2, 3, 12.0, 15.0, 10.0, 1.4);
            TrafficCarPlan left = Changer(3, 2, 15.0, 12.0, 10.0, 1.4);

            Assert.AreEqual(1, right.SignalSideAt(9.5, 0.9, 0.4));
            Assert.AreEqual(-1, left.SignalSideAt(9.5, 0.9, 0.4));
            Assert.AreEqual(0, right.SignalSideAt(8.9, 0.9, 0.4));
            Assert.AreEqual(1, right.SignalSideAt(11.7, 0.9, 0.4));
            Assert.AreEqual(0, right.SignalSideAt(11.9, 0.9, 0.4));

            // Lit the first half of each 0.66 s period, counted from when the signal starts (9.1 s).
            Assert.IsTrue(right.SignalLampAt(9.2, 0.9, 0.66));
            Assert.IsFalse(right.SignalLampAt(9.5, 0.9, 0.66));
            Assert.IsTrue(right.SignalLampAt(9.8, 0.9, 0.66));
        }

        [Test]
        public void SomeCarsChangeLane_AndNoneDoWhenTheChanceIsZero()
        {
            int changes = 0;
            for (ulong seed = 1; seed <= 10; seed++)
            {
                TrafficPlanner planner = TrafficFixtures.Planner(seed, Settings(0.3f));
                planner.PlanTo(3000);
                changes += Changers(planner.Cars).Count;

                TrafficPlanner still = TrafficFixtures.Planner(seed, Settings(0f));
                still.PlanTo(3000);
                Assert.AreEqual(0, Changers(still.Cars).Count, $"seed {seed}");
            }

            Assert.Greater(changes, 40, "ten seeds over three kilometres should see lane changes");
        }

        [Test]
        public void ACarOnlyChangesToTheNeighbourOnItsOwnSideOfTheYellowLine_AtThatLanesSpeed()
        {
            TrafficPlanner planner = TrafficFixtures.Planner(4UL, Settings(1f));
            planner.PlanTo(4000);
            var laneSpeed = new Dictionary<int, double>();
            foreach (TrafficCarPlan car in planner.Cars.Where(c => !c.HasChange))
            {
                laneSpeed[car.Lane] = car.Speed;
            }

            List<TrafficCarPlan> changers = Changers(planner.Cars);
            Assert.Greater(changers.Count, 10);
            foreach (TrafficCarPlan car in changers)
            {
                Assert.AreEqual(1, System.Math.Abs(car.Lane - car.FromLane), $"car {car.Id}");
                Assert.AreEqual(car.Lane < RoadSection.OncomingLaneCount, car.FromLane < RoadSection.OncomingLaneCount, $"car {car.Id} crossed the yellow line");
                Assert.AreEqual(laneSpeed[car.Lane], car.Speed, 1e-9, $"car {car.Id} drives its new lane's speed");
                Assert.AreEqual(laneSpeed[car.FromLane], car.FromSpeed, 1e-9, $"car {car.Id} came at its old lane's speed");
            }
        }

        [Test]
        public void EveryChangeIsOverBeforeThePlayerReachesTheCar()
        {
            TrafficSettings settings = Settings(1f);
            var profile = new PlayerSpeedProfile(new CarSettings().Speed);
            for (ulong seed = 1; seed <= 20; seed++)
            {
                TrafficPlanner planner = TrafficFixtures.Planner(seed, settings);
                planner.PlanTo(4000);

                foreach (TrafficCarPlan car in Changers(planner.Cars))
                {
                    Assert.LessOrEqual(car.ChangeEnd, profile.TimeAt(car.SigmaStart) - settings.LaneChange.MarginSeconds + 1e-6, $"seed {seed} car {car.Id}");
                    Assert.Greater(car.ChangeStart, 0.0);
                }
            }
        }

        [Test]
        public void ThePlayerIsNeverNearACarBeforeItsChangeIsOver_InAnyLane()
        {
            TrafficSettings settings = Settings(1f);
            var profile = new PlayerSpeedProfile(new CarSettings().Speed);
            int checkedCars = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                TrafficPlanner planner = TrafficFixtures.Planner(seed, settings);
                planner.PlanTo(4000);

                foreach (TrafficCarPlan car in Changers(planner.Cars))
                {
                    checkedCars++;
                    double room = (car.Length + settings.Layout.PlayerLengthMeters) * 0.5;
                    double until = profile.TimeAt(car.SigmaStart);
                    for (double t = 0.0; t <= until; t += 0.05)
                    {
                        double gap = System.Math.Abs(profile.DistanceAt(t) - car.DistanceAt(t));
                        Assert.GreaterOrEqual(gap, room, $"seed {seed} car {car.Id} at {t:F2} s");
                    }
                }
            }

            Assert.Greater(checkedCars, 50);
        }

        [Test]
        public void TwoCarsNeverShareALaneWithinTheirLengths_WhileOneChanges()
        {
            TrafficSettings settings = Settings(1f);
            for (ulong seed = 1; seed <= 5; seed++)
            {
                TrafficPlanner planner = TrafficFixtures.Planner(seed, settings);
                planner.PlanTo(2500);
                List<TrafficCarPlan> cars = planner.Cars.ToList();

                for (int a = 0; a < cars.Count; a++)
                {
                    for (int b = a + 1; b < cars.Count; b++)
                    {
                        TrafficCarPlan x = cars[a];
                        TrafficCarPlan y = cars[b];
                        if (System.Math.Abs(x.Sigma - y.Sigma) > 700.0)
                        {
                            continue;
                        }

                        double room = (x.Length + y.Length) * 0.5;
                        for (double t = 0.0; t <= 80.0; t += 0.2)
                        {
                            bool shared = Enumerable.Range(0, RoadSection.LaneCount).Any(lane => x.OccupiesLane(lane, t) && y.OccupiesLane(lane, t));
                            if (shared)
                            {
                                Assert.GreaterOrEqual(System.Math.Abs(x.DistanceAt(t) - y.DistanceAt(t)), room, $"seed {seed} cars {x.Id} and {y.Id} at {t:F1} s");
                            }
                        }
                    }
                }
            }
        }

        [Test]
        public void EverySeedCanStillBeDrivenThrough_WhenEveryCarWouldChange()
        {
            var car = new CarSettings();
            TrafficSettings settings = Settings(1f);
            int change = TrafficFixtures.ChangeMeters(settings, car);

            for (ulong seed = 1; seed <= 60; seed++)
            {
                TrafficPlanner planner = TrafficFixtures.Planner(seed, settings, car);
                planner.PlanTo(5500);

                TrafficPassability.Result result = TrafficPassability.Check(planner.Cars, car.Lateral.StartLane, 0, 5000, change);

                Assert.IsTrue(result.Passable, $"seed {seed}: the way ends at {result.ReachedMeters} m");
            }
        }

        [Test]
        public void ChangesDoNotDependOnHowTheLayoutWasAskedFor()
        {
            TrafficPlanner once = TrafficFixtures.Planner(8UL, Settings(0.5f));
            once.PlanTo(3000);
            TrafficPlanner stepwise = TrafficFixtures.Planner(8UL, Settings(0.5f));
            for (int sigma = 100; sigma <= 3000; sigma += 53)
            {
                stepwise.PlanTo(sigma);
            }

            stepwise.PlanTo(3000);

            Assert.Greater(Changers(once.Cars).Count, 10);
            var a = Sorted(once.Cars);
            var b = Sorted(stepwise.Cars);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Sigma, b[i].Sigma, 1e-9);
                Assert.AreEqual(a[i].Lane, b[i].Lane);
                Assert.AreEqual(a[i].FromLane, b[i].FromLane);
                Assert.AreEqual(a[i].ChangeStart, b[i].ChangeStart, 1e-9);
            }
        }

        private static List<TrafficCarPlan> Sorted(IReadOnlyList<TrafficCarPlan> cars)
        {
            List<TrafficCarPlan> list = cars.Where(car => car.SigmaEnd < 2800.0).ToList();
            list.Sort((x, y) => x.Lane != y.Lane ? x.Lane.CompareTo(y.Lane) : x.Sigma.CompareTo(y.Sigma));
            return list;
        }

        [Test]
        public void TheSettingsRefuseALaneChangeThatCannotBeSettledInTime()
        {
            var settings = new TrafficSettings();
            settings.LaneChange.SettleMeters = 100f;

            Assert.Throws<System.ArgumentException>(() => settings.Validate(60f, 400f));
        }
    }
}
