using UnityEngine;
using WrongLine.UI;

namespace WrongLine.Haptics
{
    /// <summary>Plays the pulse of a <see cref="HapticKind"/> on the output, if the player has haptics on and <see cref="HapticRule"/> allows it.</summary>
    public sealed class HapticsService : IHaptics
    {
        private readonly IHapticOutput _output;
        private readonly IGameplaySettings _settings;
        private readonly HapticsConfig _config;
        private readonly HapticRule _rule;

        public HapticsService(IHapticOutput output, IGameplaySettings settings, HapticsConfig config)
        {
            _output = output;
            _settings = settings;
            _config = config;
            _rule = new HapticRule(config.MinInterval);
        }

        public void Play(HapticKind kind)
        {
            // Real time, not the game clock: taps in the pause menu count too, and a slow-motion crash must not stretch the gap.
            if (!_rule.Allow(kind, Time.unscaledTime, _settings.Haptics))
            {
                return;
            }

            var pulse = _config.Get(kind);
            _output.Vibrate(pulse.Milliseconds, pulse.Amplitude);
        }
    }
}
