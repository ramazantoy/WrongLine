using System;
using System.Linq;
using AF.Core;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace AF.Platform
{
    public static class PlatformInstaller
    {
        private static readonly Type[] _contracts = { typeof(IStoreAchievements), typeof(IPlatformOverlay) };

        /// <summary>
        /// Registers the platform services in the root scope through <paramref name="backend"/>; null uses
        /// <see cref="MockPlatform"/>, the backend of the Editor and of builds without a store. Throws when the backend leaves
        /// a platform contract unregistered, and in a store build whose backend is not that store's.
        /// </summary>
        public static void UsePlatform(this IContainerBuilder builder, MessagePipeOptions messagePipe, PlatformOptions options = null,
            IPlatformBackend backend = null)
        {
            if (messagePipe == null)
                throw new ArgumentNullException(nameof(messagePipe));
            options ??= new PlatformOptions();
            backend ??= new MockPlatform();

            // A store build that fell back to the Mock would ship achievements that reach no store.
            var store = StoreBuild.Name;
            if (store != null && !string.Equals(backend.Name, store, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"This is a {store} build but UsePlatform got the {backend.Name} backend. Pass the {store} backend to UsePlatform " +
                    "in the root scope; a store build never runs another store's backend or the Mock.");

            if (builder.Exists(typeof(IAchievements), includeInterfaceTypes: true))
                throw new InvalidOperationException(
                    $"{nameof(IAchievements)} is registered before UsePlatform. It is the gate UsePlatform registers in front of the " +
                    $"backend's {nameof(IStoreAchievements)}; nothing else registers it.");

            builder.RegisterMessageBroker<PlatformOverlayChanged>(messagePipe);
            builder.RegisterInstance(options);
            backend.Install(builder, options);

            if (builder.Exists(typeof(IAchievements), includeInterfaceTypes: true))
                throw new InvalidOperationException(
                    $"The {backend.Name} platform backend registers {nameof(IAchievements)}. Backends register {nameof(IStoreAchievements)}; " +
                    $"{nameof(IAchievements)} is the gate UsePlatform puts in front of it.");

            var missing = _contracts
                .Where(contract => !builder.Exists(contract, includeInterfaceTypes: true))
                .Select(contract => contract.Name)
                .ToList();
            if (missing.Count > 0)
                throw new InvalidOperationException(
                    $"The {backend.Name} platform backend registers no {string.Join(", ", missing)}. Every backend implements " +
                    "every platform contract; register a no-op implementation where the store has no such feature.");

            builder.Register<AchievementGate>(Lifetime.Singleton).As<IAchievements>();
            if (options.PauseOnOverlay)
                builder.RegisterEntryPoint<OverlayPause>();
            builder.RegisterBuildCallback(_ => Log.Info($"Platform: {backend.Name}"));
        }
    }
}
