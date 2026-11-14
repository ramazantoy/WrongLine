using System;
using VContainer;
using VContainer.Unity;

namespace AF.Platform.Steam
{
    /// <summary>
    /// The Steam backend (Steamworks.NET). Its boot step checks the launch (started through Steam, owned) and starts the
    /// Steam API; achievements and the overlay follow it. Passed to <c>UsePlatform</c> in a Steam build only.
    /// </summary>
    public sealed class SteamPlatform : IPlatformBackend
    {
        /// <summary>Spacewar, Valve's test app.</summary>
        public const uint SpacewarAppId = 480;

        private readonly SteamOptions _options;

        /// <summary>
        /// App id 0, and <see cref="SpacewarAppId"/> outside a development player, are refused by the launch check with a
        /// message box: an exception here would stop the root scope without a word in the player.
        /// </summary>
        public SteamPlatform(SteamOptions options) => _options = options ?? throw new ArgumentNullException(nameof(options));

        public string Name => "Steam";

        public void Install(IContainerBuilder builder, PlatformOptions options)
        {
            builder.RegisterInstance(_options);
            builder.Register<SteamOverlay>(Lifetime.Singleton).As<IPlatformOverlay>().AsSelf();
            builder.Register<SteamAchievements>(Lifetime.Singleton).As<IStoreAchievements>();
            // Its boot step runs the launch check; it also pumps Steam's callbacks every frame and shuts Steam down on quit.
            builder.RegisterEntryPoint<SteamSession>().AsSelf();
        }

        /// <summary>Why the app id cannot start the game; null when it can.</summary>
        internal static string AppIdProblem(uint appId, bool developmentPlayer)
        {
            if (appId == 0)
                return "SteamOptions.AppId is not set. Write the game's Steam app id where the root scope passes the Steam backend.";
            if (appId == SpacewarAppId && !developmentPlayer)
                return $"SteamOptions.AppId is {SpacewarAppId}, Valve's test app Spacewar. Set the game's own app id before a Release build.";
            return null;
        }
    }
}
