using System;
using UnityEngine;

namespace WrongLine.World
{
    /// <summary>The numbers of the night lights: street lamps, the car's headlight and turn signal lights, and the night bloom.</summary>
    [Serializable]
    public sealed class LightsSettings
    {
        [Header("Street lamps (emission glass and a fake pool of light on the road)")]
        public LightSwitchSettings StreetLamps = new LightSwitchSettings(0.55f, 0.4f, 1.5f);

        [Tooltip("The pool of light under a lamp: its colour, how far it spreads across the ground, how far below the lamp head it reaches, its strength, and how many of the nearest lamps cast one.")]
        public Color LampPoolColor = new Color(1f, 0.91f, 0.66f, 1f);

        public float LampPoolRadius = 7f;
        public float LampPoolVertical = 9f;
        public float LampPoolStrength = 0.9f;

        [Range(0, LampPoolFalloff.MaxPools)] public int LampPoolCount = 6;

        [Header("Car (real lights: a spot headlight and a point light per turn signal side)")]
        public LightSwitchSettings Headlights = new LightSwitchSettings(0.6f, 0.45f, 0.6f);

        public Color HeadlightColor = Color.white;
        [Tooltip("Of each of the two headlight spots.")]
        public float HeadlightIntensity = 45f;

        public float HeadlightRange = 50f;

        [Range(1f, 179f)] public float HeadlightSpotAngle = 38f;

        [Tooltip("A soft point light at the car's nose that lights the road close in front and the surroundings, beside the two spot beams.")]
        public Color HeadlightGlowColor = new Color(1f, 0.96f, 0.85f, 1f);

        public float HeadlightGlowIntensity = 25f;
        public float HeadlightGlowRange = 9f;

        [Tooltip("The tail lights: one red point light at the rear, lit with the headlights.")]
        public Color TailColor = new Color(1f, 0.06f, 0.03f, 1f);

        public float TailIntensity = 14f;
        public float TailRange = 7f;

        public Color SignalColor = new Color(1f, 0.55f, 0.05f, 1f);
        public float SignalIntensity = 6f;
        public float SignalRange = 3f;

        [Header("Night bloom")]
        [Tooltip("Weight of the night bloom volume at full street lamp level (the volume's own settings are in its profile).")]
        [Range(0f, 1f)] public float NightBloomWeight = 1f;

        public void Validate()
        {
            StreetLamps.Validate();
            Headlights.Validate();

            if (!(LampPoolRadius > 0f && LampPoolVertical > 0f))
            {
                throw new ArgumentException("Lamp pool radius and vertical reach must be positive.");
            }

            if (!(LampPoolStrength >= 0f && HeadlightIntensity >= 0f && SignalIntensity >= 0f && HeadlightGlowIntensity >= 0f && TailIntensity >= 0f && HeadlightGlowRange > 0f && TailRange > 0f))
            {
                throw new ArgumentException("Light strengths must not be negative.");
            }

            if (!(LampPoolCount >= 0 && LampPoolCount <= LampPoolFalloff.MaxPools))
            {
                throw new ArgumentException($"LampPoolCount must be between 0 and {LampPoolFalloff.MaxPools}.");
            }

            if (!(HeadlightRange > 0f && SignalRange > 0f && HeadlightSpotAngle > 0f && HeadlightSpotAngle < 180f))
            {
                throw new ArgumentException("Light ranges must be positive and the spot angle between 0 and 180.");
            }
        }
    }
}
