using System;
using System.Collections.Generic;
using WrongLine.Road;

namespace WrongLine.Scenery
{
    /// <summary>A change from one biome to the next: the distance along the road at the middle of its blend band.</summary>
    public readonly struct BiomeTransition
    {
        public readonly double Distance;
        public readonly int From;
        public readonly int To;

        public BiomeTransition(double distance, int from, int to)
        {
            Distance = distance;
            From = from;
            To = to;
        }
    }

    /// <summary>
    /// Which biome the road runs through at any distance: stretches of random length (from the seed), never the same biome
    /// twice in a row, and a band at each change where the old biome fades into the new one. Made lazily as the road grows;
    /// the same seed gives the same biomes.
    /// </summary>
    public sealed class BiomeSchedule
    {
        private readonly RoadRandom _random;
        private readonly BiomeSettings _settings;
        private readonly List<double> _starts = new List<double>();
        private readonly List<int> _biomes = new List<int>();

        public BiomeSchedule(ulong seed, int biomeCount, BiomeSettings settings)
        {
            if (biomeCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(biomeCount), "There must be at least one biome.");
            }

            settings.Validate();
            _settings = settings;
            BiomeCount = biomeCount;
            _random = new RoadRandom(seed);
            _starts.Add(0.0);
            _biomes.Add(Pick(biomeCount, -1));
        }

        public int BiomeCount { get; }

        public float BlendMeters => _settings.BlendMeters;

        /// <summary>The biome whose stretch holds <paramref name="distance"/> (the change is at the middle of a blend band).</summary>
        public int BiomeAt(double distance)
        {
            return _biomes[SegmentAt(distance)];
        }

        /// <summary>How much of <paramref name="biome"/> there is at <paramref name="distance"/>: 0 to 1; all biomes add up to 1.</summary>
        public float Weight(int biome, double distance)
        {
            int segment = SegmentAt(distance);
            double half = _settings.BlendMeters * 0.5;
            double start = _starts[segment];
            double end = _starts[segment + 1];

            if (segment > 0 && distance < start + half)
            {
                float t = Blend(distance - (start - half));
                return WeightOf(biome, _biomes[segment - 1], _biomes[segment], t);
            }

            if (distance > end - half)
            {
                float t = Blend(distance - (end - half));
                return WeightOf(biome, _biomes[segment], _biomes[segment + 1], t);
            }

            return biome == _biomes[segment] ? 1f : 0f;
        }

        /// <summary>The first change whose blend band overlaps the road from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public bool TryTransition(double from, double to, out BiomeTransition transition)
        {
            Cover(to);
            double half = _settings.BlendMeters * 0.5;
            for (int i = 1; i < _starts.Count; i++)
            {
                double middle = _starts[i];
                if (middle + half >= from && middle - half <= to)
                {
                    transition = new BiomeTransition(middle, _biomes[i - 1], _biomes[i]);
                    return true;
                }
            }

            transition = default;
            return false;
        }

        private static float WeightOf(int biome, int from, int to, float t)
        {
            if (biome == from && biome == to)
            {
                return 1f; // Only one biome exists.
            }

            if (biome == from)
            {
                return 1f - t;
            }

            return biome == to ? t : 0f;
        }

        /// <summary>Smoothstep across the blend band: 0 at its start, 1 at its end.</summary>
        private float Blend(double intoBand)
        {
            float t = (float)Math.Min(1.0, Math.Max(0.0, intoBand / _settings.BlendMeters));
            return t * t * (3f - 2f * t);
        }

        /// <summary>The stretch holding <paramref name="distance"/>; the stretch after it exists too.</summary>
        private int SegmentAt(double distance)
        {
            Cover(distance);
            int low = 0;
            int high = _starts.Count - 2;
            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                if (_starts[middle] <= distance)
                {
                    low = middle;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return low;
        }

        /// <summary>Makes stretches until one starts past <paramref name="distance"/> plus a blend band.</summary>
        private void Cover(double distance)
        {
            while (_starts[_starts.Count - 1] <= distance + _settings.BlendMeters)
            {
                double length = _settings.MinSegmentMeters + _random.NextDouble() * (_settings.MaxSegmentMeters - _settings.MinSegmentMeters);
                _starts.Add(_starts[_starts.Count - 1] + length);
                _biomes.Add(Pick(BiomeCount, _biomes[_biomes.Count - 1]));
            }
        }

        private int Pick(int count, int previous)
        {
            if (count == 1 || previous < 0)
            {
                return (int)(_random.NextUInt64() % (ulong)count);
            }

            // Any biome but the previous one.
            int pick = (int)(_random.NextUInt64() % (ulong)(count - 1));
            return pick >= previous ? pick + 1 : pick;
        }
    }
}
