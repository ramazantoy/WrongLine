using System;
using UnityEngine;

namespace WrongLine.Road
{
    /// <summary>
    /// The floating origin. Scene positions are the exact double world position minus this origin, so they stay small and
    /// precise however far the road runs. When the follower gets farther than the threshold from the origin, the origin
    /// moves to the follower; everything is then placed again from the doubles (see <see cref="RoadPlacement"/>), not nudged
    /// by a float offset, because repeated float nudges would slowly pull the pieces apart.
    /// </summary>
    public sealed class OriginShift
    {
        private readonly double _threshold;

        public OriginShift(double thresholdMeters)
        {
            if (!(thresholdMeters > 0.0))
            {
                throw new ArgumentOutOfRangeException(nameof(thresholdMeters));
            }

            _threshold = thresholdMeters;
        }

        public WorldPoint Origin { get; private set; }

        /// <summary>Moves the origin to <paramref name="follower"/> if it is farther than the threshold from it.</summary>
        /// <param name="shift">How far the origin moved, so things not placed from the doubles (the camera) can follow.</param>
        public bool Update(in WorldPoint follower, out Vector3 shift)
        {
            if (follower.DistanceTo(Origin) <= _threshold)
            {
                shift = Vector3.zero;
                return false;
            }

            shift = follower.RelativeTo(Origin);
            Origin = follower;
            return true;
        }
    }
}
