using System.Collections.Generic;
using NUnit.Framework;
using WrongLine.Car;

namespace WrongLine.Traffic.Tests
{
    public class TrafficRunTests
    {
        /// <summary>Drives a run along the player's own profile in 1/30 s steps, calling <paramref name="each"/> after every step.</summary>
        private static void Drive(TrafficRun run, CarSettings car, double seconds, System.Action<double> each)
        {
            var profile = new PlayerSpeedProfile(car.Speed);
            const double Step = 1.0 / 30.0;
            run.Advance(0.0, 0.0, -1000.0);
            each(0.0);
            for (double time = Step; time <= seconds; time += Step)
            {
                run.Advance(Step, profile.DistanceAt(time), -1000.0);
                each(profile.DistanceAt(time));
            }
        }

        private static TrafficRun Run(ulong seed, TrafficSettings settings, CarSettings car)
        {
            return new TrafficRun(TrafficFixtures.Planner(seed, settings, car), settings);
        }

        [Test]
        public void CarsComeOutInsideTheSpawnRange_AndNeverBefore()
        {
            var car = new CarSettings();
            TrafficSettings settings = TrafficFixtures.Settings();
            TrafficRun run = Run(1UL, settings, car);
            int spawned = 0;

            Drive(run, car, 120.0, player =>
            {
                foreach (TrafficCarPlan plan in run.Spawned)
                {
                    spawned++;
                    double gap = plan.DistanceAt(run.Time) - player;
                    Assert.LessOrEqual(gap, settings.Stream.SpawnAheadMeters + 1e-6);
                    Assert.GreaterOrEqual(gap, -settings.Stream.DespawnBehindMeters - 1e-6);
                }
            });

            Assert.Greater(spawned, 20, "the run should meet cars");
        }

        [Test]
        public void ACarLeavesOnceItIsBehindThePlayer_AndNoActiveCarIsFurtherBack()
        {
            var car = new CarSettings();
            TrafficSettings settings = TrafficFixtures.Settings();
            TrafficRun run = Run(2UL, settings, car);
            int despawned = 0;

            Drive(run, car, 120.0, player =>
            {
                despawned += run.Despawned.Count;
                foreach (TrafficCarPlan plan in run.Active)
                {
                    Assert.GreaterOrEqual(plan.DistanceAt(run.Time), player - settings.Stream.DespawnBehindMeters - 1e-6);
                }
            });

            Assert.Greater(despawned, 20);
        }

        [Test]
        public void NothingStaysOutOnceItIsBehindTheOldestRoad()
        {
            var car = new CarSettings();
            TrafficSettings settings = TrafficFixtures.Settings();
            TrafficRun run = Run(3UL, settings, car);
            var profile = new PlayerSpeedProfile(car.Speed);

            run.Advance(0.0, 0.0, 0.0);
            double player = 0.0;
            for (double time = 1.0 / 30.0; time < 60.0; time += 1.0 / 30.0)
            {
                player = profile.DistanceAt(time);
                double oldest = player - 10.0; // Road kept behind is shorter than the despawn range.
                run.Advance(1.0 / 30.0, player, oldest);
                foreach (TrafficCarPlan plan in run.Active)
                {
                    Assert.GreaterOrEqual(plan.DistanceAt(run.Time), oldest - 1e-6);
                }
            }
        }

        [Test]
        public void NoMoreThanMaxActiveAreOut()
        {
            var car = new CarSettings();
            TrafficSettings settings = TrafficFixtures.Settings();
            settings.Stream.MaxActive = 6;
            TrafficRun run = Run(4UL, settings, car);

            Drive(run, car, 90.0, player => Assert.LessOrEqual(run.Active.Count, 6));
        }

        [Test]
        public void EveryCarThatComesOutLeavesOnce()
        {
            var car = new CarSettings();
            TrafficRun run = Run(5UL, TrafficFixtures.Settings(), car);
            var out_ = new HashSet<int>();

            Drive(run, car, 150.0, player =>
            {
                foreach (TrafficCarPlan plan in run.Spawned)
                {
                    Assert.IsTrue(out_.Add(plan.Id), $"car {plan.Id} came out twice");
                }

                foreach (int id in run.Despawned)
                {
                    Assert.IsTrue(out_.Remove(id), $"car {id} left without being out");
                }
            });

            Assert.AreEqual(run.Active.Count, out_.Count);
        }

        [Test]
        public void TheRunOnlyMovesForward()
        {
            TrafficRun run = Run(1UL, TrafficFixtures.Settings(), new CarSettings());

            Assert.Throws<System.ArgumentOutOfRangeException>(() => run.Advance(-0.1, 0.0, 0.0));
        }
    }
}
