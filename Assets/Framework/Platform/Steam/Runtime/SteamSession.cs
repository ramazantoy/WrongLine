using System;
using System.Threading;
using AF.Core;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Steamworks;
using UnityEngine;
using VContainer.Unity;

namespace AF.Platform.Steam
{
    /// <summary>
    /// The Steam API's lifetime. Its boot step runs first in the boot (<c>UsePlatform</c> is the root scope's first call):
    /// outside the Editor it lets Steam restart a game that was not launched through it, starts the API and checks the
    /// license; a refused launch quits before anything loads. Then it pumps Steam's callbacks every frame and shuts the API
    /// down when the app quits. No Steam call runs after that, nor from <c>Dispose</c>: its order against the shutdown is
    /// undefined.
    /// </summary>
    internal sealed class SteamSession : IBootStep, ITickable, IDisposable
    {
        private const string BROKEN_INSTALL = "Steam could not be loaded because the game's files are damaged. Verify the game's " +
                                              "files in Steam (Properties > Installed Files) and start it again.";
        private const string NOT_STARTED = "Steam is not running, or the game was not started through Steam. Start the game " +
                                           "from your Steam library.";
        private const string OUTDATED = "Steam is out of date. Update Steam and start the game again.";
        private const string NOT_OWNED = "This game is not available on the Steam account that is logged in.";

        private readonly SteamOptions _options;
        private readonly SteamOverlay _overlay;
        private readonly IDisposable _suspending;

        public SteamSession(SteamOptions options, SteamOverlay overlay, ISubscriber<AppSuspending> suspending)
        {
            _options = options;
            _overlay = overlay;
            _suspending = suspending.Subscribe(message =>
            {
                if (message.Reason == SuspendReason.Quit)
                    Shutdown();
            });
        }

        /// <summary>The Steam API is started and not yet shut down; Steam calls are allowed.</summary>
        public bool IsRunning { get; private set; }

        public UniTask Run(CancellationToken cancellation)
        {
            // A misconfigured app id is the developer's mistake, but it must not leave a shipped game dead without a word.
            var appIdProblem = SteamPlatform.AppIdProblem(_options.AppId, Debug.isDebugBuild);
            if (appIdProblem != null)
                return StoreLaunch.Block(appIdProblem, cancellation);

            try
            {
#if !UNITY_EDITOR
                // Before Init, which it cannot follow. True: Steam starts the game again through itself; quit silently.
                if (SteamAPI.RestartAppIfNecessary(new AppId_t(_options.AppId)))
                    return StoreLaunch.Block(null, cancellation);
#endif
                var result = SteamAPI.InitEx(out var error);
                if (result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
                {
                    Log.Error($"Steam: the API did not start ({result}: {error}).");
                    return StoreLaunch.Block(result == ESteamAPIInitResult.k_ESteamAPIInitResult_VersionMismatch ? OUTDATED : NOT_STARTED, cancellation);
                }
            }
            catch (Exception exception)
            {
                // steam_api64.dll missing (DllNotFoundException), from another SDK version (EntryPointNotFoundException), or
                // damaged: an exception here would fail the boot without a word.
                Log.Exception(exception);
                return StoreLaunch.Block(BROKEN_INSTALL, cancellation);
            }

            if (!SteamApps.BIsSubscribed())
            {
                SteamAPI.Shutdown();
                return StoreLaunch.Block(NOT_OWNED, cancellation);
            }

            IsRunning = true;
#if AF_STEAMWORKS_HARDWARE_API
            // Steamworks SDK 1.65 replaced IsSteamRunningOnSteamDeck. A Steam Machine runs the same SteamOS overlay.
            var hardware = SteamUtils.IsRunningOnSteamHardware();
            var deck = hardware == ESteamHardwareType.k_ESteamHardwareTypeSteamDeck || hardware == ESteamHardwareType.k_ESteamHardwareTypeSteamMachine;
#else
            var deck = SteamUtils.IsSteamRunningOnSteamDeck();
#endif
            _overlay.Start(treatGameInputAsClosed: deck);
            Log.Info($"Steam: started for app {SteamUtils.GetAppID()}{(deck ? " on a Steam Deck" : string.Empty)}.");
            return UniTask.CompletedTask;
        }

        public void Tick()
        {
            if (!IsRunning)
                return;
            SteamAPI.RunCallbacks();
            _overlay.Tick();
        }

        public void Dispose() => _suspending.Dispose();

        private void Shutdown()
        {
            if (!IsRunning)
                return;
            IsRunning = false;
            _overlay.Stop();
            SteamAPI.Shutdown();
        }
    }
}
