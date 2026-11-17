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
                }
            }
        }

        [Test]
        public void TimeAtIsTheInverseOfDistanceAt_OnTheRampAndAfterIt()
        {
            var profile = new PlayerSpeedProfile(new CarSettings().Speed);

            foreach (double seconds in new[] { 0.5, 1.0, 2.0, 2.25, 3.0, 10.0, 100.0 })
            {
                Assert.AreEqual(seconds, profile.TimeAt(profile.DistanceAt(seconds)), 1e-6);
            }
        }

        [Test]
        public void TheSpeedStartsAtTheStartSpeedAndSettlesAtCruise()
        {
            var car = new CarSettings();
            var profile = new PlayerSpeedProfile(car.Speed);

            Assert.AreEqual(car.Speed.StartSpeed, profile.SpeedAtDistance(0.0), 1e-9);
            Assert.AreEqual(car.Speed.CruiseSpeed, profile.SpeedAtDistance(10000.0), 1e-9);
            Assert.Greater(profile.SpeedAtDistance(20.0), car.Speed.StartSpeed);
        }
    }
}
