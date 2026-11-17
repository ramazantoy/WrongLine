using UnityEngine;
using WrongLine.Road;

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

        // Keeps the sky's draw apart from the road's: the same seed feeds both generators, which must not walk in step.
        private const ulong SeedSalt = 0xD1B54A32D192ED03UL;

        /// <summary>
        /// Where in the day a run with this seed begins: drawn from the seed between the configured phases, or the configured
        /// start phase when the random start is off. The same seed always gives the same phase.
        /// </summary>
        public float StartPhaseFor(ulong seed)
        {
            if (!_settings.RandomStart)
            {
                return _settings.StartPhase;
            }

            double draw = new RoadRandom(seed ^ SeedSalt).NextDouble();
            return Mathf.Clamp((float)(_settings.StartPhaseMin + draw * (_settings.StartPhaseMax - _settings.StartPhaseMin)), _settings.StartPhaseMin, _settings.StartPhaseMax);
        }

        /// <summary>The phase (0..1, wrapping) after this many seconds of a run that began at the configured start phase.</summary>
        public float PhaseAt(float elapsedSeconds)
        {
            return PhaseAt(elapsedSeconds, _settings.StartPhase);
        }

        /// <summary>The phase (0..1, wrapping) after this many seconds of a run that began at <paramref name="startPhase"/>.</summary>
        public float PhaseAt(float elapsedSeconds, float startPhase)
        {
            return Mathf.Repeat(startPhase + elapsedSeconds / _settings.CycleSeconds, 1f);
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
                Scale(ground, k),
                Mathf.Clamp01(_settings.Darkness.Evaluate(t)));
        }

        private static Color Scale(Color color, float k)
        {
            return new Color(color.r * k, color.g * k, color.b * k, 1f);
        }
    }
}
