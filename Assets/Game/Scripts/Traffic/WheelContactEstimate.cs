using System;
using System.Collections.Generic;
using UnityEngine;

namespace WrongLine.Traffic
{
    /// <summary>
    /// Where a car's wheels touch the road, read from a mesh whose wheels are part of the body (a model without separate wheel nodes):
    /// the vertices near the ground are sorted into the four corners and each corner's middle is a contact. The axles are the middles of
    /// the front and the rear pair along the car's length, the half track is half the distance between a pair. Pure.
    /// </summary>
    public static class WheelContactEstimate
    {
        public readonly struct Result
        {
            public Result(float frontAxle, float rearAxle, float halfTrack)
            {
                FrontAxle = frontAxle;
                RearAxle = rearAxle;
                HalfTrack = halfTrack;
            }

            public float FrontAxle { get; }

            public float RearAxle { get; }

            public float HalfTrack { get; }
        }

        /// <param name="vertices">The mesh's vertices in the car's space: +Z forward, +X right, the road at y = 0.</param>
        /// <param name="groundBand">How far above the road (metres) a vertex still counts as touching it.</param>
        /// <returns>False when a corner has no vertex near the ground (the mesh does not stand on four wheels).</returns>
        public static bool TryEstimate(IReadOnlyList<Vector3> vertices, float groundBand, out Result result)
        {
            if (vertices == null)
            {
                throw new ArgumentNullException(nameof(vertices));
            }

            // The corners are sorted by the middle of the ground contact: the car's own length and width say where it is.
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;
            var near = new List<Vector3>();
            foreach (Vector3 v in vertices)
            {
                if (v.y <= groundBand)
                {
                    near.Add(v);
                    minZ = Math.Min(minZ, v.z);
                    maxZ = Math.Max(maxZ, v.z);
                }
            }

            result = default;
            if (near.Count == 0)
            {
                return false;
            }

            float middleZ = (minZ + maxZ) * 0.5f;
            var sumZ = new float[4];
            var sumX = new float[4];
            var count = new int[4];
            foreach (Vector3 v in near)
            {
                int corner = (v.z >= middleZ ? 0 : 2) + (v.x >= 0f ? 1 : 0);
                sumZ[corner] += v.z;
                sumX[corner] += v.x;
                count[corner]++;
            }

            for (int i = 0; i < 4; i++)
            {
                if (count[i] == 0)
                {
                    return false;
                }
            }

            float frontLeftZ = sumZ[0] / count[0];
            float frontRightZ = sumZ[1] / count[1];
            float rearLeftZ = sumZ[2] / count[2];
            float rearRightZ = sumZ[3] / count[3];
            float frontLeftX = sumX[0] / count[0];
            float frontRightX = sumX[1] / count[1];
            float rearLeftX = sumX[2] / count[2];
            float rearRightX = sumX[3] / count[3];

            float halfFront = Math.Abs(frontRightX - frontLeftX) * 0.5f;
            float halfRear = Math.Abs(rearRightX - rearLeftX) * 0.5f;
            result = new Result((frontLeftZ + frontRightZ) * 0.5f, (rearLeftZ + rearRightZ) * 0.5f, (halfFront + halfRear) * 0.5f);
            return true;
        }
    }
}
