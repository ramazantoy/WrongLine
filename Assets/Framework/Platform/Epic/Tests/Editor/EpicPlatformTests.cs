using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AF.Core;
using MessagePipe;
using NUnit.Framework;
using VContainer;

namespace AF.Platform.Epic.Tests
{
    public class EpicPlatformTests
    {
        private string _directory;

        [SetUp]
        public void SetUp() => _directory = Path.Combine(Path.GetTempPath(), "EpicPlatformTests-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }

        [Test]
        public void Backend_RegistersEveryContract_AndItsBootStep()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<PauseChanged>(messagePipe);
            builder.Register<IPauseService, PauseService>(Lifetime.Singleton);

            builder.UsePlatform(messagePipe, new PlatformOptions(), new EpicPlatform());

            using var container = builder.Build();
            Assert.IsInstanceOf<EpicAchievements>(container.Resolve<IStoreAchievements>());
            Assert.IsInstanceOf<EpicOverlay>(container.Resolve<IPlatformOverlay>());
            Assert.IsInstanceOf<EpicSession>(container.Resolve<IReadOnlyList<IBootStep>>().Single());
        }

        [Test]
        public void UnlockBeforeLogin_WaitsInTheQueue_WithoutReachingEos()
        {
            var achievements = new EpicAchievements();
            achievements.Start(new EpicUnlockQueue(_directory, "deployment", "account"));

            // An EOS call here would need a running EOSManager.
            Assert.DoesNotThrow(() => achievements.Unlock("ACH_A"));

            Assert.IsFalse(achievements.IsUnlocked("ACH_A"));
            CollectionAssert.AreEqual(new[] { "ACH_A" }, new EpicUnlockQueue(_directory, "deployment", "account").Pending);
        }

        [Test]
        public void UnlockWithoutAnAccount_IsIgnored()
        {
            var achievements = new EpicAchievements();
            achievements.Start(new EpicUnlockQueue(_directory, null, null));

            Assert.DoesNotThrow(() => achievements.Unlock("ACH_A"));
            Assert.IsFalse(achievements.IsUnlocked("ACH_A"));
            Assert.IsFalse(Directory.Exists(_directory));
        }

        [Test]
        public void Overlay_CountsOnlyChanges()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<PlatformOverlayChanged>(messagePipe);
            builder.Register<EpicOverlay>(Lifetime.Singleton);
            using var container = builder.Build();
            var overlay = container.Resolve<EpicOverlay>();
            var messages = new List<bool>();
            using var subscription = container.Resolve<ISubscriber<PlatformOverlayChanged>>().Subscribe(change => messages.Add(change.IsOpen));

            overlay.Set(true);
            overlay.Set(true);
            overlay.Set(false);

            CollectionAssert.AreEqual(new[] { true, false }, messages);
        }
    }
}
