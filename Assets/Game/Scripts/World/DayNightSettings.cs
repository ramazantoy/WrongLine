using System;
using UnityEngine;

namespace WrongLine.World
{
    /// <summary>
    /// The numbers of the day and night cycle. Phase runs 0..1 and wraps: 0 sunrise, 0.25 noon, 0.5 sunset, 0.75 midnight.
    /// The gradients and the curve are read along the phase, so the first and last key of each should match (the cycle is a loop).
    /// </summary>
    [Serializable]
    public sealed class DayNightSettings
    {
        [Tooltip("Seconds for one full day and night.")]
        public float CycleSeconds = 240f;

        [Tooltip("Where in the day a run begins (0 sunrise, 0.25 noon, 0.5 sunset, 0.75 midnight).")]
        [Range(0f, 1f)] public float StartPhase = 0.15f;

        [Tooltip("Colour straight above the camera.")]
        public Gradient Zenith = Make((0.00f, 0.38f, 0.48f, 0.78f), (0.25f, 0.20f, 0.50f, 0.95f), (0.50f, 0.36f, 0.26f, 0.58f), (0.62f, 0.05f, 0.06f, 0.20f), (0.75f, 0.02f, 0.03f, 0.10f), (0.90f, 0.10f, 0.13f, 0.32f), (1.00f, 0.38f, 0.48f, 0.78f));

        [Tooltip("Colour at the horizon. The fog takes exactly this colour, so the road fades into the sky without a seam.")]
        public Gradient Horizon = Make((0.00f, 1.00f, 0.62f, 0.45f), (0.25f, 0.75f, 0.88f, 1.00f), (0.50f, 1.00f, 0.45f, 0.35f), (0.62f, 0.22f, 0.13f, 0.32f), (0.75f, 0.06f, 0.08f, 0.18f), (0.90f, 0.35f, 0.30f, 0.45f), (1.00f, 1.00f, 0.62f, 0.45f));

        [Tooltip("Colour below the horizon.")]
        public Gradient Ground = Make((0.00f, 0.30f, 0.25f, 0.30f), (0.25f, 0.45f, 0.50f, 0.55f), (0.50f, 0.30f, 0.20f, 0.25f), (0.75f, 0.03f, 0.03f, 0.06f), (1.00f, 0.30f, 0.25f, 0.30f));

        [Tooltip("Colour of the sun and, at night, the moon light.")]
        public Gradient Light = Make((0.00f, 1.00f, 0.70f, 0.50f), (0.25f, 1.00f, 0.96f, 0.86f), (0.50f, 1.00f, 0.50f, 0.35f), (0.62f, 0.40f, 0.45f, 0.80f), (0.75f, 0.45f, 0.50f, 0.90f), (0.90f, 0.50f, 0.50f, 0.80f), (1.00f, 1.00f, 0.70f, 0.50f));

        [Tooltip("Light intensity along the phase.")]
        public AnimationCurve LightIntensity = SmoothCurve((0f, 0.7f), (0.25f, 1.1f), (0.5f, 0.7f), (0.6f, 0.25f), (0.75f, 0.2f), (0.9f, 0.25f), (1f, 0.7f));

        [Tooltip("How far the light is tilted down at its lowest (degrees). The light always shines down, the moon included.")]
        public float MinLightPitch = 15f;

        [Tooltip("How far the light is tilted down at its highest (degrees).")]
        public float MaxLightPitch = 70f;

        [Tooltip("The light's heading (degrees).")]
        public float LightYaw = 330f;

        [Tooltip("Scales the ambient colours (taken from the sky) before they light the scene.")]
        public float AmbientIntensity = 0.6f;

        public void Validate()
        {
            if (!(CycleSeconds > 0f))
            {
                throw new ArgumentException("CycleSeconds must be positive.");
            }

            if (!(StartPhase >= 0f && StartPhase <= 1f))
            {
                throw new ArgumentException("StartPhase must be between 0 and 1.");
            }

            if (!(MinLightPitch >= 0f && MaxLightPitch <= 90f && MinLightPitch <= MaxLightPitch))
            {
                throw new ArgumentException("Light pitch must satisfy 0 <= MinLightPitch <= MaxLightPitch <= 90.");
            }

            if (!(AmbientIntensity >= 0f))
            {
                throw new ArgumentException("AmbientIntensity must not be negative.");
            }
        }

        private static Gradient Make(params (float Time, float R, float G, float B)[] keys)
        {
            var colorKeys = new GradientColorKey[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                colorKeys[i] = new GradientColorKey(new Color(keys[i].R, keys[i].G, keys[i].B), keys[i].Time);
            }

            var gradient = new Gradient();
            gradient.SetKeys(colorKeys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        private static AnimationCurve SmoothCurve(params (float Time, float Value)[] keys)
        {
            var curve = new AnimationCurve();
            foreach ((float time, float value) in keys)
            {
                curve.AddKey(time, value);
            }

            for (int i = 0; i < curve.length; i++)
            {
                curve.SmoothTangents(i, 0f);
            }

            return curve;
        }
    }
}
