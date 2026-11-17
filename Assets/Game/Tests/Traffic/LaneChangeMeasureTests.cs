using NUnit.Framework;
using WrongLine.Car;

namespace WrongLine.Traffic.Tests
{
    public class LaneChangeMeasureTests
    {
        [Test]
        public void ALaneChangeTakesAboutAThirdOfASecond_AndTheSettingsLaneChangeTimeCoversIt()
        {
            float seconds = LaneChangeMeasure.Seconds(new CarSettings());

            Assert.That(seconds, Is.InRange(0.2f, 0.6f));
            Assert.LessOrEqual(seconds, TrafficFixtures.Settings().Layout.LaneChangeSeconds);
        }

        [Test]
        public void ASlowerSpring_TakesLonger()
        {
            var slow = new CarSettings();
            slow.Lateral.SmoothTime = 0.24f;

            Assert.Greater(LaneChangeMeasure.Seconds(slow), LaneChangeMeasure.Seconds(new CarSettings()) * 1.5f);
        }
    }
}
