using UnityEngine;

namespace WrongLine.Audio
{
    /// <summary>
    /// Turns the car's speed into the engine's revs, pitch and volume. The speed sweeps the revs from idle to full inside a gear;
    /// when the speed passes a gear's top the target drops to idle and the revs fall to it at a limited rate (the shift). Slow motion
    /// lowers the pitch with the game's time scale; a crash lets the sound die away. Pure apart from <see cref="Mathf"/>.
    /// </summary>
    public sealed class EngineSoundRule
    {
        private const float MinPitch = 0.1f;

        private readonly EngineSoundSettings _settings;
        private float _revs;
        private bool _started;
        private float _crashSeconds;

        public EngineSoundRule(EngineSoundSettings settings)
        {
            _settings = settings;
        }

        public EngineSound Sound { get; private set; }

        /// <summary>The gear the speed is in, counted from 0.</summary>
        public int GearAt(float speed)
        {
            float[] tops = _settings.GearTopSpeeds;
            for (int i = 0; i < tops.Length; i++)
            {
                if (speed < tops[i])
                {
                    return i;
                }
            }

            return tops.Length - 1;
        }

        public void Update(float deltaTime, float speed, float lateralSpeed, float timeScale, bool crashed)
        {
            float[] tops = _settings.GearTopSpeeds;
            int gear = GearAt(speed);
            float bottom = gear == 0 ? 0f : tops[gear - 1];
            float share = Mathf.Clamp01((speed - bottom) / (tops[gear] - bottom));
            float target = Mathf.Lerp(_settings.IdleRevs, 1f, share)
                + _settings.LateralRevs * Mathf.Clamp01(Mathf.Abs(lateralSpeed) / _settings.LateralFullSpeed);
            target = Mathf.Clamp01(target);

            if (!_started)
            {
                _revs = target;
                _started = true;
            }
            else if (target > _revs)
            {
                _revs = Mathf.Min(target, _revs + _settings.RiseRate * deltaTime);
            }
            else
            {
                _revs = Mathf.Max(target, _revs - _settings.FallRate * deltaTime);
            }

            float alive = 1f;
            if (crashed)
            {
                _crashSeconds += deltaTime;
                alive = 1f - Mathf.Clamp01(_crashSeconds / _settings.CrashFadeSeconds);
            }

            float speedShare = Mathf.Clamp01(speed / _settings.FullVolumeSpeed);
            float volume = _settings.MasterVolume * Mathf.Lerp(_settings.IdleVolume, 1f, speedShare) * alive;
            float high = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_settings.CrossfadeFrom, _settings.CrossfadeTo, _revs));
            float pitch = Mathf.Max(MinPitch, Mathf.Lerp(_settings.PitchAtIdle, _settings.PitchAtRedline, _revs) * timeScale);

            float windShare = Mathf.Clamp01(speed / _settings.WindFullSpeed);
            Sound = new EngineSound(
                _revs,
                pitch,
                volume * (1f - high),
                volume * high,
                _settings.WindVolume * windShare * windShare * alive,
                Mathf.Max(MinPitch, Mathf.Lerp(_settings.WindPitchMin, _settings.WindPitchMax, windShare) * timeScale));
        }
    }
}
