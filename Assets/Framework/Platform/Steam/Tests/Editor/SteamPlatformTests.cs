using System;
using System.Collections.Generic;
using System.Linq;
using AF.Core;
using MessagePipe;
using NUnit.Framework;
using VContainer;

namespace AF.Platform.Steam.Tests
{
    public class SteamPlatformTests
    {
        [Test]
        public void AppIdZero_IsRefused() =>
            StringAssert.Contains("AppId is not set", SteamPlatform.AppIdProblem(0, developmentPlayer: true));

        [Test]
        public void Spacewar_IsOnlyForDevelopmentPlayers()
        {
            Assert.IsNull(SteamPlatform.AppIdProblem(SteamPlatform.SpacewarAppId, developmentPlayer: true));
            StringAssert.Contains("Spacewar", SteamPlatform.AppIdProblem(SteamPlatform.SpacewarAppId, developmentPlayer: false));
        }

        [Test]
        public void GameAppId_IsAccepted() => Assert.IsNull(SteamPlatform.AppIdProblem(1234560, developmentPlayer: false));

        [Test]
        public void UnsetAppId_DoesNotStopTheRootScope() => Assert.DoesNotThrow(() => new SteamPlatform(new SteamOptions()));

        [Test]
        public void Backend_RegistersEveryContract_AndItsBootStep()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<PauseChanged>(messagePipe);
            builder.RegisterMessageBroker<AppSuspending>(messagePipe);
            builder.Register<IPauseService, PauseService>(Lifetime.Singleton);

            var options = new PlatformOptions { AchievementIds = new[] { "ACH_WIN_ONE_GAME" } };
            builder.UsePlatform(messagePipe, options, new SteamPlatform(new SteamOptions { AppId = SteamPlatform.SpacewarAppId }));

            using var container = builder.Build();
            Assert.IsInstanceOf<SteamAchievements>(container.Resolve<IStoreAchievements>());
            Assert.IsInstanceOf<SteamOverlay>(container.Resolve<IPlatformOverlay>());
            Assert.IsInstanceOf<SteamSession>(container.Resolve<IReadOnlyList<IBootStep>>().Single());
            // Not started: nothing reaches Steam.
            Assert.IsFalse(container.Resolve<SteamSession>().IsRunning);
            Assert.DoesNotThrow(() => container.Resolve<IAchievements>().Unlock("ACH_WIN_ONE_GAME"));
        }

        [Test]
        public void AchievementsBeforeSteamRuns_DoNotReachSteam()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<AppSuspending>(messagePipe);
            builder.RegisterMessageBroker<PlatformOverlayChanged>(messagePipe);
            builder.RegisterInstance(new SteamOptions { AppId = SteamPlatform.SpacewarAppId });
            builder.Register<SteamOverlay>(Lifetime.Singleton);
            builder.Register<SteamSession>(Lifetime.Singleton);
            builder.Register<SteamAchievements>(Lifetime.Singleton);
            using var container = builder.Build();
            var achievements = container.Resolve<SteamAchievements>();

            // A Steam call before the API runs would throw "Steamworks is not initialized".
            Assert.DoesNotThrow(() => achievements.Unlock("ACH_WIN_ONE_GAME"));
            Assert.IsFalse(achievements.IsUnlocked("ACH_WIN_ONE_GAME"));
        }
    }
}
