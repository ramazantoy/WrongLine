using UnityEngine;

namespace WrongLine.World
{
    /// <summary>
    /// A set of lights that follows the darkness of the day: it turns on once the darkness reaches the on threshold and off once
    /// it falls to the off threshold (between the two it keeps its state, a hysteresis), and its level fades smoothly to the new
    /// state. The first update puts the lights straight in their state, so a run that begins at night starts lit. Pure; time only
    /// moves with the delta time, so a pause (delta 0) holds the fade.
    /// </summary>
    public sealed class LightSwitchRule
    {
        private readonly LightSwitchSettings _settings;
        private bool _started;
        private bool _on;
        private float _progress;

        public LightSwitchRule(LightSwitchSettings settings)
        {
            settings.Validate();
            _settings = settings;
        }

        /// <summary>Whether the lights are meant to be on (the target; <see cref="Level"/> may still be fading to it).</summary>
        public bool On => _on;

        /// <summary>How lit the lights are, 0 (off) to 1 (fully on), easing in and out.</summary>
        public float Level => _progress * _progress * (3f - 2f * _progress);

        public void Update(float darkness, float deltaTime)
        {
            if (!_started)
            {
                _on = darkness >= _settings.OnDarkness;
                _progress = _on ? 1f : 0f;
                _started = true;
                return;
            }

            if (!_on && darkness >= _settings.OnDarkness)
            {
                _on = true;
            }
            else if (_on && darkness <= _settings.OffDarkness)
            {
                _on = false;
            }

            float step = deltaTime / _settings.FadeSeconds;
            _progress = Mathf.Clamp01(_progress + (_on ? step : -step));
        }
    }
}
