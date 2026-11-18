using NUnit.Framework;

namespace WrongLine.Crash.Tests
{
    public class WreckRuleTests
    {
        private CrashSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new CrashSettings();
        }

        [Test]
        public void TheSameCrashBreaksTheSameWay()
        {
            var a = new WreckRule(_settings.Wreck, 42, 20f, 1f, 1);
            var b = new WreckRule(_settings.Wreck, 42, 20f, 1f, 1);

            for (int part = 0; part < 12; part++)
            {
                Assert.AreEqual(a.Launch(part).Linear, b.Launch(part).Linear);
                Assert.AreEqual(a.Launch(part).Angular, b.Launch(part).Angular);
            }
        }

        [Test]
        public void PartsAndSeedsFlyDifferently()
        {
            var rule = new WreckRule(_settings.Wreck, 42, 20f, 1f, 1);
            var other = new WreckRule(_settings.Wreck, 43, 20f, 1f, 1);

            Assert.AreNotEqual(rule.Launch(0).Linear, rule.Launch(1).Linear);
            Assert.AreNotEqual(rule.Launch(0).Linear, other.Launch(0).Linear);
        }

        [Test]
        public void PartsFlyTheWayTheCarWasGoingAndNotFasterThanTheSpeedShareAllows()
        {
            var ahead = new WreckRule(_settings.Wreck, 5, 30f, 1f, 1);
            var behind = new WreckRule(_settings.Wreck, 5, 30f, -1f, 1);
            float cap = 30f * _settings.Wreck.SpeedShare * 1.4f;

            for (int part = 0; part < 50; part++)
            {
                Assert.Greater(ahead.Launch(part).Linear.z, 0f);
                Assert.LessOrEqual(ahead.Launch(part).Linear.z, cap + 1e-3f);
                Assert.Less(behind.Launch(part).Linear.z, 0f);
            }
        }

        [Test]
        public void PartsAreThrownUpAndMostlyAwayFromTheCarThatWasHit()
        {
            var hitOnRight = new WreckRule(_settings.Wreck, 9, 30f, 1f, 1);
            float across = 0f;
            for (int part = 0; part < 200; part++)
            {
                PartLaunch launch = hitOnRight.Launch(part);
                Assert.Greater(launch.Linear.y, 0f);
                Assert.LessOrEqual(launch.Linear.y, _settings.Wreck.UpKick + 1e-3f);
                across += launch.Linear.x;
            }

            Assert.Less(across, 0f, "the car hit was on the right: parts fly to the left");
        }

        [Test]
        public void NoClosingSpeedMeansNoForwardThrow()
        {
            var rule = new WreckRule(_settings.Wreck, 5, -10f, 1f, 1);

            for (int part = 0; part < 10; part++)
            {
                Assert.AreEqual(0f, rule.Launch(part).Linear.z);
            }
        }

        [Test]
        public void TheSpinIsWithinTheConfiguredLimit()
        {
            var rule = new WreckRule(_settings.Wreck, 5, 30f, 1f, -1);

            for (int part = 0; part < 50; part++)
            {
                Assert.LessOrEqual(rule.Launch(part).Angular.magnitude, _settings.Wreck.SpinDegrees + 1e-2f);
            }
        }
    }
}
