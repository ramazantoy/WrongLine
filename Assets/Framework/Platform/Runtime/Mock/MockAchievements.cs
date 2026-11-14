using System;
using System.Collections.Generic;
using AF.Core;

namespace AF.Platform
{
    /// <summary>
    /// Achievements kept in memory for the session. Unlocking goes through <see cref="IAchievements"/>, whose gate stops
    /// unknown ids; the Mock's own type only lists and resets, so a tool cannot unlock past the check.
    /// </summary>
    public sealed class MockAchievements : IStoreAchievements
    {
        private readonly HashSet<string> _unlocked = new HashSet<string>(StringComparer.Ordinal);

        public IReadOnlyCollection<string> Unlocked => _unlocked;

        void IStoreAchievements.Unlock(string id)
        {
            if (_unlocked.Add(id))
                Log.Info($"Platform (mock): achievement '{id}' unlocked");
        }

        public bool IsUnlocked(string id) => _unlocked.Contains(id);

        /// <summary>Locks every achievement again, like a store's developer reset.</summary>
        public void ResetAll() => _unlocked.Clear();
    }
}
