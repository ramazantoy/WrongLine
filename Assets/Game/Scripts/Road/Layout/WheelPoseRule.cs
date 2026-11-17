using UnityEngine;

namespace WrongLine.Road
{
    /// <summary>
    /// Where a car stands and how it is turned so all four wheels touch the road. A car placed with the road's frame at
    /// its middle only touches it there: where the road bends up or down (the steep pieces) or its banking changes along the car
    /// (banked pieces), the wheels float on one side or sink on the other (measured: up to 47 cm for the truck on BankSharp). This
    /// reads the road under each wheel and puts the car on those four points. Pure.
    /// </summary>
    public static class WheelPoseRule
    {
        /// <param name="distance">Where the car's pivot is along the road, in metres.</param>
        /// <param name="lateral">Metres right of the centre line (the road's right) the car's middle is.</param>
        /// <param name="direction">+1 for a car driving the way the road runs, −1 for one coming the other way (it faces back along the road).</param>
        /// <param name="origin">Positions are returned relative to it (the scene origin).</param>
        public static void At(IRoadPath path, double distance, float lateral, int direction, float frontAxle, float rearAxle, float halfTrack, in WorldPoint origin, out Vector3 position, out Quaternion rotation)
        {
            // On a bend a lane is longer or shorter than the centre line (outside or inside), so a metre of car is not a metre of the
            // distance along the road: the axles' offsets are divided by how much road lane there is per metre of that distance here.
            double ratio = LaneLengthRatio(path, distance, lateral, origin);

            // The car's right is the road's left when it comes the other way.
            double front = distance + direction * frontAxle / ratio;
            double rear = distance + direction * rearAxle / ratio;
            float right = lateral + direction * halfTrack;
            float left = lateral - direction * halfTrack;

            Vector3 frontRight = Wheel(path, front, right, origin);
            Vector3 frontLeft = Wheel(path, front, left, origin);
            Vector3 rearRight = Wheel(path, rear, right, origin);
            Vector3 rearLeft = Wheel(path, rear, left, origin);

            Vector3 forward = ((frontRight + frontLeft) - (rearRight + rearLeft)).normalized;
            Vector3 across = (frontRight + rearRight) - (frontLeft + rearLeft);
            Vector3 up = Vector3.Cross(forward, across).normalized;
            rotation = Quaternion.LookRotation(forward, up);

            // The four contact points average out to the middle of the wheels; the pivot is behind or ahead of it by the axles' offset.
            position = (frontRight + frontLeft + rearRight + rearLeft) * 0.25f - forward * ((frontAxle + rearAxle) * 0.5f);
        }

        /// <summary>Metres of lane per metre of distance along the road at <paramref name="distance"/>, <paramref name="lateral"/> metres from the centre line.</summary>
        private static double LaneLengthRatio(IRoadPath path, double distance, float lateral, in WorldPoint origin)
        {
            Vector3 behind = Wheel(path, distance - 1.0, lateral, origin);
            Vector3 ahead = Wheel(path, distance + 1.0, lateral, origin);
            return Mathf.Clamp(Vector3.Distance(behind, ahead) * 0.5f, 0.3f, 3f);
        }

        private static Vector3 Wheel(IRoadPath path, double distance, float lateral, in WorldPoint origin)
        {
            return RoadSampler.At(path, distance, lateral).Position.RelativeTo(origin);
        }
    }
}
