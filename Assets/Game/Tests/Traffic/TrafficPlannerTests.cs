using System.Collections.Generic;
using NUnit.Framework;
using WrongLine.Car;
using WrongLine.Road;

namespace WrongLine.Traffic.Tests
{
    public class TrafficPlannerTests
    {
        private const int Seeds = 200;
        private const int Kilometres = 5000;

        [Test]
        public void EverySeedCanBeDrivenThroughFromTheStartLaneToTheEnd()
        {
            var car = new CarSettings();
            TrafficSettings settings = TrafficFixtures.Settings();
            int change = TrafficFixtures.ChangeMeters(settings, car);

            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                TrafficPlanner planner = TrafficFixtures.Planner(seed, settings, car);
                planner.PlanTo(Kilometres + 500);

                TrafficPassability.Result result = TrafficPassability.Check(planner.Cars, car.Lateral.StartLane, 0, Kilometres, change);

                Assert.IsTrue(result.Passable, $"seed {seed}: the way ends at {result.ReachedMeters} m");
            }
        }

        [Test]
        public void NoSliceHasAllLanesFull()
        {
            TrafficPlanner planner = TrafficFixtures.Planner(5UL);
            planner.PlanTo(Kilometres);

            for (int sigma = 150; sigma < Kilometres; sigma++)
            {
                int full = 0;
                for (int lane = 0; lane < RoadSection.LaneCount; lane++)
                {
                    foreach (TrafficCarPlan plan in planner.Cars)
                    {
                        if (plan.Lane == lane && plan.SigmaStart <= sigma && plan.SigmaEnd >= sigma)
                        {
                            full++;
                            break;
                        }
                    }
                }

                Assert.LessOrEqual(full, RoadSection.LaneCount - 1, $"σ {sigma}");
            }
        }

        [Test]
        public void TheCorridorLaneIsAlwaysClear()
        {
            TrafficPlanner planner = TrafficFixtures.Planner(11UL);
            planner.PlanTo(Kilometres);

            foreach (TrafficCarPlan plan in planner.Cars)
            {
                Assert.IsFalse(planner.Corridor.Reserves(plan.Lane, plan.SigmaStart, plan.SigmaEnd), $"car {plan.Id} in lane {plan.Lane} at σ {plan.Sigma:F0}");
            }
        }

        [Test]
        public void ASeedGivesTheSameTrafficHoweverItIsAskedFor()
        {
            TrafficPlanner once = TrafficFixtures.Planner(7UL);
            once.PlanTo(3000);
            TrafficPlanner stepwise = TrafficFixtures.Planner(7UL);
            for (int sigma = 100; sigma <= 3000; sigma += 37)
            {
                stepwise.PlanTo(sigma);
            }

            stepwise.PlanTo(3000);

            var a = Sorted(once.Cars, 3000);
            var b = Sorted(stepwise.Cars, 3000);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Sigma, b[i].Sigma, 1e-9);
                Assert.AreEqual(a[i].Lane, b[i].Lane);
                Assert.AreEqual(a[i].Model, b[i].Model);
            }
        }

        [Test]
        public void DifferentSeedsGiveDifferentTraffic()
        {
            TrafficPlanner a = TrafficFixtures.Planner(1UL);
            TrafficPlanner b = TrafficFixtures.Planner(2UL);
            a.PlanTo(1000);
            b.PlanTo(1000);

            Assert.AreNotEqual(a.Cars[0].Sigma, b.Cars[0].Sigma);
        }

        [Test]
        public void NothingStartsInTheClearStretch()
        {
            TrafficSettings settings = TrafficFixtures.Settings();
            for (ulong seed = 1; seed <= 30; seed++)
            {
                TrafficPlanner planner = TrafficFixtures.Planner(seed, settings);
                planner.PlanTo(1000);

                foreach (TrafficCarPlan plan in planner.Cars)
                {
                    Assert.GreaterOrEqual(plan.SigmaStart, settings.Layout.ClearStartMeters - 1e-9, $"seed {seed}");
                }
            }
        }

        [Test]
        public void ALaneDrivesOneSpeed_OncomingLanesTheOtherWay()
        {
            TrafficPlanner planner = TrafficFixtures.Planner(3UL);
            planner.PlanTo(2000);
            var speeds = new Dictionary<int, double>();

            foreach (TrafficCarPlan plan in planner.Cars)
            {
                if (speeds.TryGetValue(plan.Lane, out double speed))
                {
                    Assert.AreEqual(speed, plan.Speed, 1e-9, $"lane {plan.Lane}");
                }
                else
                {
                    speeds.Add(plan.Lane, plan.Speed);
                }

                Assert.AreEqual(plan.Lane < RoadSection.OncomingLaneCount, plan.Speed < 0.0, $"lane {plan.Lane}");
            }
        }

        [Test]
        public void ACarIsWhereThePlanSaysWhenThePlayerReachesSigma()
        {
            var car = new CarSettings();
            var profile = new PlayerSpeedProfile(car.Speed);
            TrafficPlanner planner = TrafficFixtures.Planner(4UL, null, car);
            planner.PlanTo(2000);

            foreach (TrafficCarPlan plan in planner.Cars)
            {
                double time = profile.TimeAt(plan.Sigma);
                Assert.AreEqual(plan.Sigma, plan.DistanceAt(time), 1e-6, $"car {plan.Id}");
            }
        }

        private static List<TrafficCarPlan> Sorted(IReadOnlyList<TrafficCarPlan> cars, double below)
        {
            var list = new List<TrafficCarPlan>();
            foreach (TrafficCarPlan plan in cars)
            {
                if (plan.SigmaEnd < below)
                {
                    list.Add(plan);
                }
            }

            list.Sort((x, y) => x.Lane != y.Lane ? x.Lane.CompareTo(y.Lane) : x.Sigma.CompareTo(y.Sigma));
            return list;
        }
    }
}
