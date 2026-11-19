using System;
using UnityEngine;

namespace WrongLine.Traffic
{
    /// <summary>
    /// Finds which way a generated car model faces, from the model's own paint. The file's axes are not known (a model may come
    /// Y-up or Z-up, nose along either way): the car is longer than it is wide or tall, which gives the length axis; the headlights are
    /// at the nose and the brake lights at the tail, which gives the way it faces; the tyres are the dark parts and sit low, which
    /// gives the up axis. The result is the car's own frame in the file's coordinates. Pure.
    /// </summary>
    public static class CarFrameEstimate
    {
        /// <summary>How far apart (of the car's length) the headlight and brake light must lie to say which end is the nose.</summary>
        private const float MinNoseSeparation = 0.1f;

        /// <summary>How far (of the car's height) the tyres must lie off the middle on an axis for it to be called the up axis.</summary>
        private const float MinDarkOffset = 0.05f;

        /// <param name="boundsCenter">The middle of the mesh's bounds, in the file's axes.</param>
        /// <param name="boundsSize">The size of the mesh's bounds, in the file's axes.</param>
        /// <param name="headlightMean">The mean position of the surface painted as a headlight.</param>
        /// <param name="stopMean">The mean position of the surface painted as a brake light.</param>
        /// <param name="darkMean">The mean position of the very dark surface (tyres, underbody).</param>
        /// <param name="right">The car's right, a unit vector in the file's axes.</param>
        /// <param name="up">The car's up.</param>
        /// <param name="forward">The car's nose direction.</param>
        /// <returns>False when the markers do not say (the lights are not told apart, or the dark parts are not low).</returns>
        public static bool TryEstimate(Vector3 boundsCenter, Vector3 boundsSize, Vector3 headlightMean, Vector3 stopMean, Vector3 darkMean, out Vector3 right, out Vector3 up, out Vector3 forward)
        {
            right = Vector3.right;
            up = Vector3.up;
            forward = Vector3.forward;

            int length = LongestAxis(boundsSize);
            float lengthSize = boundsSize[length];
            if (!(lengthSize > 0f))
            {
                return false;
            }

            float nose = headlightMean[length] - stopMean[length];
            if (Math.Abs(nose) < lengthSize * MinNoseSeparation)
            {
                return false;
            }

            // Of the two axes left, the tyres are low on one (their middle is off the car's middle) and level on the other.
            int first = (length + 1) % 3;
            int second = (length + 2) % 3;
            float firstOffset = Math.Abs(darkMean[first] - boundsCenter[first]) / boundsSize[first];
            float secondOffset = Math.Abs(darkMean[second] - boundsCenter[second]) / boundsSize[second];
            int vertical = firstOffset >= secondOffset ? first : second;
            float offset = Math.Max(firstOffset, secondOffset);
            if (offset < MinDarkOffset)
            {
                return false;
            }

            forward = Axis(length, nose > 0f ? 1f : -1f);
            up = Axis(vertical, darkMean[vertical] < boundsCenter[vertical] ? 1f : -1f);
            right = Vector3.Cross(up, forward);
            return true;
        }

        private static int LongestAxis(Vector3 size)
        {
            int axis = 0;
            if (size.y > size[axis])
            {
                axis = 1;
            }

            if (size.z > size[axis])
            {
                axis = 2;
            }

            return axis;
        }

        private static Vector3 Axis(int index, float sign)
        {
            var axis = Vector3.zero;
            axis[index] = sign;
            return axis;
        }
    }
}
