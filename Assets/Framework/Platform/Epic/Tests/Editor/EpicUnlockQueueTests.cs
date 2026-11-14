using System;
using System.IO;
using NUnit.Framework;

namespace AF.Platform.Epic.Tests
{
    public class EpicUnlockQueueTests
    {
        private string _directory;

        [SetUp]
        public void SetUp() => _directory = Path.Combine(Path.GetTempPath(), "EpicUnlockQueueTests-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }

        [Test]
        public void Unlocks_SurviveARestart_UntilConfirmed()
        {
            var queue = new EpicUnlockQueue(_directory, "deployment-1", "account1");
            queue.Add("ACH_A");
            queue.Add("ACH_B");
            queue.Add("ACH_A");

            var restarted = new EpicUnlockQueue(_directory, "deployment-1", "account1");
            CollectionAssert.AreEqual(new[] { "ACH_A", "ACH_B" }, restarted.Pending);

            restarted.Remove("ACH_A");
            restarted.Remove("ACH_B");

            Assert.IsFalse(Directory.Exists(_directory) && Directory.GetFiles(_directory).Length > 0, "A confirmed queue leaves no file");
            CollectionAssert.IsEmpty(new EpicUnlockQueue(_directory, "deployment-1", "account1").Pending);
        }

        [Test]
        public void EachAccount_HasItsOwnFile()
        {
            new EpicUnlockQueue(_directory, "deployment", "accountA").Add("ACH_A");

            CollectionAssert.IsEmpty(new EpicUnlockQueue(_directory, "deployment", "accountB").Pending);
            Assert.IsTrue(File.Exists(Path.Combine(_directory, "deployment_accountA.json")));
        }

        [TestCase(null, "account")]
        [TestCase("deployment", "")]
        [TestCase("../..", "account")]
        public void WithoutLauncherIds_TheQueueIsInactive(string deployment, string account)
        {
            var queue = new EpicUnlockQueue(_directory, deployment, account);
            queue.Add("ACH_A");

            Assert.IsFalse(queue.IsActive);
            CollectionAssert.IsEmpty(queue.Pending);
            Assert.IsFalse(Directory.Exists(_directory));
        }

        [Test]
        public void UnreadableFile_IsKept_AndTheQueueStartsEmpty()
        {
            Directory.CreateDirectory(_directory);
            var path = Path.Combine(_directory, "deployment_account.json");
            File.WriteAllText(path, "{ not json");
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("could not be read"));

            var queue = new EpicUnlockQueue(_directory, "deployment", "account");

            CollectionAssert.IsEmpty(queue.Pending);
            Assert.IsTrue(File.Exists(path));
        }
    }
}
