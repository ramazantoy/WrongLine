namespace AF.Platform
{
    /// <summary>
    /// The store's achievements, as game code sees them: a gate that checks ids in front of the backend's
    /// <see cref="IStoreAchievements"/>. The game's own save data is the truth and the store mirrors it: after loading, unlock
    /// again what the save says was earned, because a store that was not ready yet may have missed it. Unlocking is idempotent.
    /// </summary>
    public interface IAchievements
    {
        /// <summary>
        /// Unlocks the achievement; an unlocked one stays as it is. An id missing from
        /// <see cref="PlatformOptions.AchievementIds"/> is logged as an error and ignored.
        /// </summary>
        void Unlock(string id);

        /// <summary>An id missing from <see cref="PlatformOptions.AchievementIds"/> is logged as an error and reads as locked.</summary>
        bool IsUnlocked(string id);
    }
}
