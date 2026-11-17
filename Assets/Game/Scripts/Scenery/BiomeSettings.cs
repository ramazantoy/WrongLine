using System;
using UnityEngine;

namespace WrongLine.Scenery
{
    /// <summary>How long each biome lasts along the road and how wide the band where one turns into the next is.</summary>
    [Serializable]
    public sealed class BiomeSettings
    {
        [Tooltip("Shortest stretch of road one biome covers, in metres. Keep it longer than the road window, so at most one change is in sight.")]
        [SerializeField] private float _minSegmentMeters = 1500f;

        [Tooltip("Longest stretch of road one biome covers, in metres.")]
        [SerializeField] private float _maxSegmentMeters = 3000f;

        [Tooltip("Length of road over which one biome fades into the next (ground colour and props), in metres.")]
        [SerializeField] private float _blendMeters = 150f;

        public BiomeSettings()
        {
        }

        public BiomeSettings(float minSegmentMeters, float maxSegmentMeters, float blendMeters)
        {
            _minSegmentMeters = minSegmentMeters;
            _maxSegmentMeters = maxSegmentMeters;
            _blendMeters = blendMeters;
        }

        public float MinSegmentMeters => _minSegmentMeters;

        public float MaxSegmentMeters => _maxSegmentMeters;

        public float BlendMeters => _blendMeters;

        /// <exception cref="ArgumentException">A number makes no sense.</exception>
        public void Validate()
        {
            if (!(_blendMeters > 0f))
            {
                throw new ArgumentException("BlendMeters must be positive.");
            }

            if (!(_minSegmentMeters > _blendMeters) || _maxSegmentMeters < _minSegmentMeters)
            {
                throw new ArgumentException("A biome must last longer than its blend (MinSegmentMeters > BlendMeters), and MaxSegmentMeters must not be below MinSegmentMeters.");
            }
        }
    }
}
