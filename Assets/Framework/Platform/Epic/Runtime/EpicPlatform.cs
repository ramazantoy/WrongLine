using VContainer;
using VContainer.Unity;

namespace AF.Platform.Epic
{
    /// <summary>
    /// The Epic Games Store backend (PlayEveryWare's EOS plugin). Its boot step checks that the Epic Games launcher started
    /// the game and starts EOS; the login runs in the background and never holds the boot. Achievements wait in a queue file
    /// until Epic confirms them. The product's identity (product, sandbox, deployment, client) is in the plugin's
    /// configuration under <c>StreamingAssets/EOS</c>, so the backend takes no options. Passed to <c>UsePlatform</c> in an
    /// Epic build only.
    /// </summary>
    public sealed class EpicPlatform : IPlatformBackend
    {
        public string Name => "Epic";

        public void Install(IContainerBuilder builder, PlatformOptions options)
        {
            builder.Register<EpicOverlay>(Lifetime.Singleton).As<IPlatformOverlay>().AsSelf();
            builder.Register<EpicAchievements>(Lifetime.Singleton).As<IStoreAchievements>().AsSelf();
            builder.RegisterEntryPoint<EpicSession>().AsSelf();
        }
    }
}
