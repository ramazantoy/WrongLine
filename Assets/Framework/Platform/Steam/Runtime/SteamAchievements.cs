using AF.Core;
using Steamworks;

namespace AF.Platform.Steam
{
    /// <summary>
    /// Steam's achievements. The Steam client keeps unlocks while offline and syncs them itself; the game's save stays the
    /// truth and the game unlocks again after loading. Ignored while the Steam API is not running.
    /// </summary>
    internal sealed class SteamAchievements : IStoreAchievements
    {
        private readonly SteamSession _session;

        public SteamAchievements(SteamSession session) => _session = session;

        public void Unlock(string id)
        {
            if (!_session.IsRunning)
            {
                Log.Warning($"Steam: not running, {id} was not unlocked. The game unlocks it again after its next load.");
                return;
            }
            if (!SteamUserStats.GetAchievement(id, out var achieved))
            {
                Log.Error($"Steam: this app has no achievement with the API name {id}. Check it in Steamworks and in AchievementIds.");
                return;
            }
            if (achieved)
                return;
            if (!SteamUserStats.SetAchievement(id))
            {
                Log.Error($"Steam: {id} could not be unlocked.");
                return;
            }
            // Sends it to Steam; on failure the client sends it with the next StoreStats.
            if (!SteamUserStats.StoreStats())
                Log.Error($"Steam: {id} was unlocked but not stored yet.");
            Log.Info($"Steam: unlocked {id}.");
        }

        public bool IsUnlocked(string id) => _session.IsRunning && SteamUserStats.GetAchievement(id, out var achieved) && achieved;
    }
}
