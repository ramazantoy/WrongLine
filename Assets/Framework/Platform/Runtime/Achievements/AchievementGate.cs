using System;
using System.Collections.Generic;
using AF.Core;

namespace AF.Platform
{
    /// <summary>
    /// The game's <see cref="IAchievements"/>, in front of every backend's <see cref="IStoreAchievements"/>. Stores ignore ids
    /// they do not define without a word; the gate logs them as errors, whatever the backend, so a typo shows up in the
    /// Editor on the Mock already.
    /// </summary>
    internal sealed class AchievementGate : IAchievements
    {
        private readonly IStoreAchievements _store;
        private readonly HashSet<string> _known;

        public AchievementGate(PlatformOptions options, IStoreAchievements store)
        {
            _store = store;
            _known = new HashSet<string>(options.AchievementIds ?? Array.Empty<string>(), StringComparer.Ordinal);
        }

        public void Unlock(string id)
        {
            if (IsKnown(id))
                _store.Unlock(id);
        }

        public bool IsUnlocked(string id) => IsKnown(id) && _store.IsUnlocked(id);

        private bool IsKnown(string id)
        {
            if (id != null && _known.Contains(id))
                return true;
            Log.Error($"Platform: unknown achievement id '{id}'. Add it to PlatformOptions.AchievementIds; a store ignores ids it does not define.");
            return false;
        }
    }
}
