using System;
using UnityEngine;

namespace WrongLine.Haptics
{
    /// <summary>One vibration: how long and how strong (1 to 255).</summary>
    [Serializable]
    public struct HapticPulse
    {
        [Min(1)] public int Milliseconds;
        [Range(1, 255)] public int Amplitude;
    }

    /// <summary>The pulse of each <see cref="HapticKind"/> and the shortest gap between two that are not harder than the last.</summary>
    [CreateAssetMenu(menuName = "WrongLine/Haptics Config")]
    public sealed class HapticsConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _minInterval = 0.06f;
        [SerializeField] private HapticPulse _tick = new HapticPulse { Milliseconds = 8, Amplitude = 40 };
        [SerializeField] private HapticPulse _tap = new HapticPulse { Milliseconds = 15, Amplitude = 90 };
        [SerializeField] private HapticPulse _bump = new HapticPulse { Milliseconds = 45, Amplitude = 160 };
        [SerializeField] private HapticPulse _crash = new HapticPulse { Milliseconds = 320, Amplitude = 255 };

        public float MinInterval => _minInterval;

        public HapticPulse Get(HapticKind kind)
        {
            switch (kind)
            {
                case HapticKind.Tick:
                    return _tick;
                case HapticKind.Tap:
                    return _tap;
                case HapticKind.Bump:
                    return _bump;
                default:
                    return _crash;
            }
        }
    }
}
