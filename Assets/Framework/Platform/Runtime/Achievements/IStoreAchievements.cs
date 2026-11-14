namespace AF.Platform
{
    /// <summary>
    /// A store's own achievements, implemented by each platform backend. Game code never sees it: <c>UsePlatform</c> puts the
    /// achievement gate in front of it as <see cref="IAchievements"/>, so only ids listed in
    /// <see cref="PlatformOptions.AchievementIds"/> reach the store.
    /// </summary>
    public interface IStoreAchievements
    {
        /// <summary>Unlocks the achievement in the store. Must be idempotent: the game unlocks earned achievements again after loading.</summary>
        void Unlock(string id);

        bool IsUnlocked(string id);
    }
}
