using System;
using System.Collections.Generic;
using UnityEngine;
using WrongLine.World;

namespace WrongLine.Scenery
{
    /// <summary>Picks the street lamps nearest the camera, across the ground: the few the shader can give a pool of light to. Allocates nothing per call.</summary>
    public sealed class LampPicker
    {
        private readonly float[] _distances = new float[LampPoolFalloff.MaxPools];

        /// <summary>
        /// Writes the nearest <paramref name="max"/> candidates, nearest first, into <paramref name="result"/>; on a tie the earlier
        /// candidate comes first.
        /// </summary>
        /// <returns>How many it wrote (fewer than <paramref name="max"/> when there are fewer candidates).</returns>
        public int Pick(IReadOnlyList<Vector3> candidates, Vector3 from, int max, Vector3[] result)
        {
            if (max < 0 || max > LampPoolFalloff.MaxPools || max > result.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(max), $"max must be between 0 and {LampPoolFalloff.MaxPools} and fit the result array.");
            }

            int count = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                Vector3 candidate = candidates[i];
                float dx = candidate.x - from.x;
                float dz = candidate.z - from.z;
                float distance = dx * dx + dz * dz;

                // Where it would go: after every picked lamp that is nearer or as near (ties keep the earlier one first).
                int slot = count;
                while (slot > 0 && _distances[slot - 1] > distance)
                {
                    slot--;
                }

                if (slot >= max)
                {
                    continue;
                }

                int last = Math.Min(count, max - 1);
                for (int j = last; j > slot; j--)
                {
                    _distances[j] = _distances[j - 1];
                    result[j] = result[j - 1];
                }

                _distances[slot] = distance;
                result[slot] = candidate;
                count = Math.Min(count + 1, max);
            }

            return count;
        }
    }
}
