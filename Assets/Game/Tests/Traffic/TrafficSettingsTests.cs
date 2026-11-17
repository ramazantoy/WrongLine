using System;
using NUnit.Framework;

namespace WrongLine.Traffic.Tests
{
    public class TrafficSettingsTests
    {
        [Test]
        public void TheDefaultsAreValid()
        {
            Assert.DoesNotThrow(() => new TrafficSettings().Validate(60f, 400f));
        }

        [Test]
        public void ACarMustLeaveBeforeItsRoadPieceDoes()
        {
            var settings = new TrafficSettings();
            settings.Stream.DespawnBehindMeters = 60f;

            Assert.Throws<ArgumentException>(() => settings.Validate(60f, 400f));
        }

        [Test]
        public void CarsMustComeOutInsideTheRoadAhead()
        {
            var settings = new TrafficSettings();
            settings.Stream.SpawnAheadMeters = 420f;

            Assert.Throws<ArgumentException>(() => settings.Validate(60f, 400f));
        }

        [Test]
        public void ALaneAsFastAsThePlayerIsRefused()
        {
            var settings = new TrafficSettings();
            settings.Layout.LaneSpeedMax = 0.9f;

            Assert.Throws<ArgumentException>(() => settings.Validate(60f, 400f));
        }
    }
}
