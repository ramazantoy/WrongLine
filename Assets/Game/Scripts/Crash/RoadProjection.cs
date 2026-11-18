using UnityEngine;
using WrongLine.Road;

namespace WrongLine.Crash
{
    /// <summary>
    /// How far along the road a point in the scene is: the distance of the nearest point on the road's centre line. Searched
    /// around where it was last, since a wrecked car's parts only move so far between frames. Pure.
    /// </summary>
    public static class RoadProjection
    {
        private const int RefineSteps = 30;

        /// <param name="point">A scene position (relative to <paramref name="origin"/>).</param>
        /// <param name="hint">A distance near the answer, in metres.</param>
        /// <param name="window">How far either side of <paramref name="hint"/> to look.</param>
        /// <param name="step">The spacing of the first, coarse look.</param>
        public static double DistanceOf(IRoadPath path, in WorldPoint origin, Vector3 point, double hint, double window = 60.0, double step = 1.0)
        {
            double best = hint;
            float bestSquared = float.MaxValue;
            for (double s = hint - window; s <= hint + window; s += step)
            {
                float squared = Squared(path, origin, point, s);
                if (squared < bestSquared)
                {
                    bestSquared = squared;
                    best = s;
                }
            }

            // Ternary search for the lowest point between the neighbours of the best coarse sample.
            double low = best - step;
            double high = best + step;
            for (int i = 0; i < RefineSteps; i++)
            {
                double a = low + (high - low) / 3.0;
                double b = high - (high - low) / 3.0;
                if (Squared(path, origin, point, a) < Squared(path, origin, point, b))
                {
                    high = b;
                }
                else
                {
                    low = a;
                }
            }

            return (low + high) * 0.5;
        }

        private static float Squared(IRoadPath path, in WorldPoint origin, Vector3 point, double distance)
        {
            return (RoadSampler.At(path, distance).Position.RelativeTo(origin) - point).sqrMagnitude;
        }
    }
}
