using VContainer;

namespace AF.Platform
{
    /// <summary>
    /// One store or platform: Mock, Steam, Epic, later others. <see cref="Install"/> registers an implementation of every
    /// platform contract (<see cref="IStoreAchievements"/>, <see cref="IPlatformOverlay"/>); <c>UsePlatform</c> fails when one
    /// is missing, so a store without a feature registers a no-op implementation. A backend that starts asynchronously
    /// registers an <c>IBootStep</c>.
    /// </summary>
    public interface IPlatformBackend
    {
        /// <summary>
        /// For logs and reports. A store backend uses the store's name as the build menu spells it (Steam, Epic): a store
        /// build refuses a backend with another name.
        /// </summary>
        string Name { get; }

        void Install(IContainerBuilder builder, PlatformOptions options);
    }
}
