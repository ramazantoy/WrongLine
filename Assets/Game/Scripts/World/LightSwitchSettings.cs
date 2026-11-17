using System;
using UnityEngine;

namespace WrongLine.World
{
    /// <summary>When a set of lights (the street lamps, the car's headlights) turns on and off, and how long it takes to fade.</summary>
    [Serializable]
    public sealed class LightSwitchSettings
    {
        [Tooltip("The lights turn on when the darkness (0 day, 1 night) reaches this.")]
        [Range(0f, 1f)] public float OnDarkness = 0.55f;

        [Tooltip("The lights turn off when the darkness falls to this. Below OnDarkness: between the two the lights keep their state, so dusk does not flicker them.")]
        [Range(0f, 1f)] public float OffDarkness = 0.4f;

        [Tooltip("Seconds for the lights to fade from off to fully on, or back.")]
        public float FadeSeconds = 1.5f;

        public LightSwitchSettings()
        {
        }

        public LightSwitchSettings(float onDarkness, float offDarkness, float fadeSeconds)
        {
            OnDarkness = onDarkness;
            OffDarkness = offDarkness;
            FadeSeconds = fadeSeconds;
        }

        public void Validate()
        {
            if (!(OffDarkness >= 0f && OffDarkness < OnDarkness && OnDarkness <= 1f))
            {
                throw new ArgumentException("Light switch thresholds must satisfy 0 <= OffDarkness < OnDarkness <= 1.");
            }

            if (!(FadeSeconds > 0f))
            {
                throw new ArgumentException("FadeSeconds must be positive.");
            }
        }
    }
}
