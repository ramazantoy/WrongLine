using UnityEngine;

namespace WrongLine.World
{
    /// <summary>The day and night cycle as a rule: how long the run has lasted in, what the sky looks like out.</summary>
    public sealed class DayNightRule
    {
        private readonly DayNightSettings _settings;

        public DayNightRule(DayNightSettings settings)
        {
            settings.Validate();
            _settings = settings;
        }

        /// <summary>The phase (0..1, wrapping) after this many seconds of the run.</summary>
        public float PhaseAt(float elapsedSeconds)
        {
            return Mathf.Repeat(_settings.StartPhase + elapsedSeconds / _settings.CycleSeconds, 1f);
        }

        public SkyState Evaluate(float phase)
        {
            float t = Mathf.Repeat(phase, 1f);
            Color zenith = _settings.Zenith.Evaluate(t);
            Color horizon = _settings.Horizon.Evaluate(t);
            Color ground = _settings.Ground.Evaluate(t);

            // The body in the sky is highest at noon and midnight, lowest at the horizons: |sin| of the phase.
            float height = Mathf.Abs(Mathf.Sin(t * 2f * Mathf.PI));
            float pitch = Mathf.Lerp(_settings.MinLightPitch, _settings.MaxLightPitch, height);

            float k = _settings.AmbientIntensity;
            return new SkyState(
                zenith,
                horizon,
                ground,
                _settings.Light.Evaluate(t),
                Mathf.Max(0f, _settings.LightIntensity.Evaluate(t)),
                pitch,
                _settings.LightYaw,
                Scale(zenith, k),
                Scale(horizon, k),
                Scale(ground, k));
        }

        private static Color Scale(Color color, float k)
        {
            return new Color(color.r * k, color.g * k, color.b * k, 1f);
        }
    }
}
