using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Platform.Tests
{
    public class MockAchievementsTests
    {
        private MockAchievements _store;
        private IAchievements _achievements;

        [SetUp]
        public void SetUp()
        {
            _store = new MockAchievements();
            _achievements = new AchievementGate(new PlatformOptions { AchievementIds = new[] { "first-win", "collector" } }, _store);
        }

        [Test]
        public void Unlock_IsIdempotent()
        {
            _achievements.Unlock("first-win");
            _achievements.Unlock("first-win");

            Assert.IsTrue(_achievements.IsUnlocked("first-win"));
            Assert.IsFalse(_achievements.IsUnlocked("collector"));
            Assert.AreEqual(1, _store.Unlocked.Count);
        }

        [Test]
        public void UnknownId_IsLoggedAndIgnored()
        {
            LogAssert.Expect(LogType.Error, new Regex("unknown achievement id 'frist-win'"));

            _achievements.Unlock("frist-win");

            Assert.IsFalse(_store.IsUnlocked("frist-win"));
            Assert.AreEqual(0, _store.Unlocked.Count);
        }

        [Test]
        public void ResetAll_LocksEverything()
        {
            _achievements.Unlock("first-win");
            _store.ResetAll();

            Assert.IsFalse(_achievements.IsUnlocked("first-win"));
        }
    }
}
