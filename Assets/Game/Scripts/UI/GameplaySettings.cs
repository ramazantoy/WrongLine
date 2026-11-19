using MessagePipe;

namespace WrongLine.UI
{
    /// <summary>The game's own settings, kept in PlayerPrefs (language and volumes stay with the framework's save). Setters write at once and publish.</summary>
    public sealed class GameplaySettings : IGameplaySettings
    {
        private const string HAPTICS_KEY = "gameplay.haptics";

        private readonly IPublisher<GameplaySettingsChanged> _changed;
        private readonly IPrefsStore _store;
        private bool _haptics;

        public GameplaySettings(IPublisher<GameplaySettingsChanged> changed, IPrefsStore store)
        {
            _changed = changed;
            _store = store;
            _haptics = _store.GetInt(HAPTICS_KEY, 1) != 0;
        }

        public bool Haptics => _haptics;

        public void SetHaptics(bool on)
        {
            if (_haptics == on)
            {
                return;
            }

            _haptics = on;
            _store.SetInt(HAPTICS_KEY, on ? 1 : 0);
            _changed.Publish(new GameplaySettingsChanged());
        }
    }
}
