using System;
using System.Threading;
using AF.Core;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Logging;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;
using ConnectLoginInfo = Epic.OnlineServices.Connect.LoginCallbackInfo;
using Object = UnityEngine.Object;

namespace AF.Platform.Epic
{
    /// <summary>
    /// EOS's lifetime. Its boot step runs first in the boot (<c>UsePlatform</c> is the root scope's first call): outside the
    /// Editor it quits when the Epic Games launcher did not start the game, then it starts the plugin's <c>EOSManager</c>.
    /// The login (the launcher's exchange code, then Connect) runs in the background: an offline player or a slow consent
    /// screen never holds the boot, and achievements wait in their queue. EOS failing to start never stops the game; its
    /// services stay inactive. The plugin shuts EOS down when the app quits.
    /// </summary>
    internal sealed class EpicSession : IBootStep, IDisposable
    {
        private const string NOT_FROM_LAUNCHER = "This game must be started from the Epic Games Launcher.";

        private readonly EpicOverlay _overlay;
        private readonly EpicAchievements _achievements;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();

        public EpicSession(EpicOverlay overlay, EpicAchievements achievements)
        {
            _overlay = overlay;
            _achievements = achievements;
        }

        public UniTask Run(CancellationToken cancellation)
        {
#if !UNITY_EDITOR
            if (!EpicLaunchArguments.FromLauncher(Environment.GetCommandLineArgs()))
                return StoreLaunch.Block(NOT_FROM_LAUNCHER, cancellation);
            // EOS logs heavily at Log level; stack traces on those would flood Player.log.
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
#endif
            // The command line only: the queue keeps this session's unlocks even when EOS does not start.
            var launcher = EOSManager.EOSSingleton.GetCommandLineArgsFromEpicLauncher();
            _achievements.Start(new EpicUnlockQueue(EpicAchievements.QueueDirectory, launcher.epicDeploymentID, launcher.epicUserID));
            try
            {
                StartEos();
            }
            catch (Exception exception)
            {
                Log.Exception(exception);
                return UniTask.CompletedTask;
            }

            _overlay.Start();
            Login(launcher.authPassword, _lifetime.Token).Forget();
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
        }

        private static void StartEos()
        {
            if (Object.FindAnyObjectByType<EOSManager>() != null)
                return;
#if (UNITY_STANDALONE_WIN && !UNITY_EDITOR) || UNITY_EDITOR_WIN
            // The plugin sometimes has no Windows specifics yet when EOSManager wakes; registering twice throws.
            if (EOSManagerPlatformSpecificsSingleton.Instance == null)
            {
                try
                {
                    WindowsPlatformSpecifics.Register();
                }
                catch (Exception exception)
                {
                    Log.Warning($"Epic: Windows platform specifics: {exception.Message}");
                }
            }
#endif
            // EOSManager initializes EOS in Awake and keeps itself across scenes.
            new GameObject(nameof(EOSManager)).AddComponent<EOSManager>();
            ApplyLogLevelsByName();
        }

        /// <summary>
        /// The plugin applies its log level configuration by list position, which misses categories (HTTP stays at
        /// VeryVerbose); this applies it again by name.
        /// </summary>
        private static void ApplyLogLevelsByName()
        {
            try
            {
                // Throws instead of returning null when the native platform failed to load.
                if (EOSManager.Instance.GetEOSPlatformInterface() == null)
                    return;
                var pairs = Config.Get<LogLevelConfig>()?.LogCategoryLevelPairs;
                if (pairs == null)
                    return;
                foreach (var pair in pairs)
                {
                    if (Enum.TryParse(pair.Category, out LogCategory category) && category != LogCategory.AllCategories &&
                        Enum.TryParse(pair.Level, out LogLevel level))
                        EOSManager.Instance.SetLogLevel(category, level);
                }
            }
            catch (Exception exception)
            {
                Log.Warning($"Epic: log levels: {exception.Message}");
            }
        }

        private async UniTaskVoid Login(string exchangeCode, CancellationToken cancellation)
        {
            if (string.IsNullOrEmpty(exchangeCode))
            {
                // The offline launcher, or -EpicPortal alone in a local test: no login, the queue keeps the unlocks.
                Log.Warning("Epic: no exchange code from the launcher; not logging in.");
                return;
            }
            try
            {
                if (EOSManager.Instance.GetEOSPlatformInterface() == null)
                {
                    Log.Error("Epic: EOS did not start (missing configuration or native files); not logging in.");
                    return;
                }
                // No time limit: nothing waits for the login, and the first one waits for the player to accept the app's
                // permissions. The exchange code works once: no retry with it.
                if (!await LoginChain(exchangeCode, cancellation))
                    return;
                var user = EOSManager.Instance.GetProductUserId();
                if (user == null || !user.IsValid())
                {
                    Log.Error("Epic: logged in without a product user id.");
                    return;
                }
                Log.Info("Epic: logged in.");
                await _achievements.OnLoggedIn(user, EOSManager.Instance.GetLocalUserId());
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Log.Exception(exception);
            }
        }

        /// <summary>
        /// Auth with the exchange code, then Connect. Connect's InvalidUser is a first login: create the user, then log in
        /// once more (only that call registers the plugin's auth expiration handler).
        /// </summary>
        private static async UniTask<bool> LoginChain(string exchangeCode, CancellationToken cancellation)
        {
            var auth = new UniTaskCompletionSource<Result>();
            EOSManager.Instance.StartLoginWithLoginTypeAndToken(LoginCredentialType.ExchangeCode, null, exchangeCode, info =>
            {
                // An intermediate result comes first when the SDK retries by itself; only the final one counts.
                if (info.ResultCode.IsOperationComplete())
                    auth.TrySetResult(info.ResultCode);
            });
            var authResult = await auth.Task;
            if (authResult != Result.Success)
            {
                Log.Error($"Epic: auth login failed ({authResult}).");
                return false;
            }
            cancellation.ThrowIfCancellationRequested();

            var connect = await ConnectLogin();
            if (connect.ResultCode == Result.InvalidUser)
            {
                var created = new UniTaskCompletionSource<Result>();
                EOSManager.Instance.CreateConnectUserWithContinuanceToken(connect.ContinuanceToken, info =>
                {
                    if (info.ResultCode.IsOperationComplete())
                        created.TrySetResult(info.ResultCode);
                });
                var createdResult = await created.Task;
                if (createdResult != Result.Success)
                {
                    Log.Error($"Epic: the Connect user could not be created ({createdResult}).");
                    return false;
                }
                cancellation.ThrowIfCancellationRequested();
                connect = await ConnectLogin();
            }
            if (connect.ResultCode != Result.Success)
            {
                Log.Error($"Epic: Connect login failed ({connect.ResultCode}).");
                return false;
            }
            return true;
        }

        private static UniTask<ConnectLoginInfo> ConnectLogin()
        {
            var completion = new UniTaskCompletionSource<ConnectLoginInfo>();
            EOSManager.Instance.StartConnectLoginWithEpicAccount(EOSManager.Instance.GetLocalUserId(), info =>
            {
                if (info.ResultCode.IsOperationComplete())
                    completion.TrySetResult(info);
            });
            return completion.Task;
        }
    }
}
