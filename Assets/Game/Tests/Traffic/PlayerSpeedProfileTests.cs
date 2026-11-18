using NUnit.Framework;
using WrongLine.Car;

namespace WrongLine.Traffic.Tests
{
    public class PlayerSpeedProfileTests
    {
        [Test]
        public void ItAgreesWithTheSpeedRuleSteppedFrameByFrame()
        {
            var car = new CarSettings();
            var rule = new SpeedRule(car);
            var profile = new PlayerSpeedProfile(car.Speed);

            double distance = 0.0;
            const float Step = 1f / 60f;
            for (int frame = 1; frame <= 60 * 20; frame++)
            {
                rule.Update(Step);
                distance += rule.Speed * Step;
                if (frame % 60 == 0)
                {
                    Assert.AreEqual(distance, profile.DistanceAt(frame * (double)Step), 1.0, $"after {frame} frames");
                    Assert.AreEqual(profile.DistanceAt(frame * (double)Step), rule.Distance, 1e-3, $"rule at frame {frame}");
                }
            }
        }

        [Test]
        public void TimeAtIsTheInverseOfDistanceAt_InAllThreeStretches()
        {
            var car = new CarSettings();
            var profile = new PlayerSpeedProfile(car.Speed);
            double gainEnd = (car.Speed.CruiseSpeed - car.Speed.StartSpeed) / car.Speed.Acceleration + (car.Speed.MaxSpeed - car.Speed.CruiseSpeed) / car.Speed.GainPerSecond;

            foreach (double seconds in new[] { 0.5, 1.0, 2.0, 2.25, 3.0, 10.0, 100.0, gainEnd - 1.0, gainEnd, gainEnd + 1.0, gainEnd + 100.0 })
            {
                Assert.AreEqual(seconds, profile.TimeAt(profile.DistanceAt(seconds)), 1e-6, $"{seconds} s");
            }
        }

        [Test]
        public void TheSpeedStartsAtTheStartSpeedPassesTheCruiseSpeedAndSettlesAtTheTopSpeed()
        {
            var car = new CarSettings();
            var profile = new PlayerSpeedProfile(car.Speed);

            Assert.AreEqual(car.Speed.StartSpeed, profile.SpeedAtDistance(0.0), 1e-9);
            Assert.Greater(profile.SpeedAtDistance(20.0), car.Speed.StartSpeed);
            Assert.AreEqual(car.Speed.MaxSpeed, profile.SpeedAtDistance(1000000.0), 1e-9);
            Assert.AreEqual(car.Speed.MaxSpeed, profile.Top, 1e-9);
            Assert.AreEqual(car.Speed.CruiseSpeed, profile.Cruise, 1e-9);
        }

        [Test]
        public void SpeedIsContinuousAndNeverFalls_AndSpeedAtTimeAndAtDistanceAgree()
        {
            var car = new CarSettings();
            var profile = new PlayerSpeedProfile(car.Speed);

            double last = profile.SpeedAtTime(0.0);
            for (double t = 0.01; t < 400.0; t += 0.01)
            {
                double speed = profile.SpeedAtTime(t);
                Assert.GreaterOrEqual(speed, last - 1e-12, $"falls at {t} s");
                Assert.Less(speed - last, 0.1, $"jumps at {t} s");
                Assert.AreEqual(speed, profile.SpeedAtDistance(profile.DistanceAt(t)), 1e-6, $"at {t} s");
                last = speed;
            }
        }

        [Test]
        public void TheDistanceIsTheIntegralOfTheSpeed()
        {
            var profile = new PlayerSpeedProfile(new CarSettings().Speed);

            // A midpoint sum over 1 ms steps, across the ramp, the gaining stretch and into the top speed.
            double sum = 0.0;
            for (double t = 0.0005; t < 250.0; t += 0.001)
            {
                sum += profile.SpeedAtTime(t) * 0.001;
            }

            Assert.AreEqual(sum, profile.DistanceAt(250.0), 0.5);
        }

        [Test]
        public void TheCorridorSliceIsLongEnoughForALaneChangeAtTheTopSpeed()
        {
            var car = new CarSettings();
            var profile = new PlayerSpeedProfile(car.Speed);
            TrafficSettings settings = TrafficFixtures.Settings();

            double slice = TrafficPlanner.SliceLength(settings, profile);

            Assert.GreaterOrEqual(slice, profile.Top * settings.Layout.LaneChangeSeconds * 1.25 + 5.0 - 1e-9);
        }

        [Test]
        public void WithoutGainTheCruiseSpeedIsTheTop()
        {
            var car = new CarSettings();
            car.Speed.GainPerSecond = 0f;
            var profile = new PlayerSpeedProfile(car.Speed);

            Assert.AreEqual(car.Speed.CruiseSpeed, profile.Top, 1e-9);
            Assert.AreEqual(car.Speed.CruiseSpeed, profile.SpeedAtDistance(100000.0), 1e-9);
            Assert.AreEqual(50.0, profile.TimeAt(profile.DistanceAt(50.0)), 1e-6);
        }
    }
}
