using UnityEngine;
using WrongLine.Road;

namespace WrongLine.Driving
{
    /// <summary>
    /// What the chase camera needs to know this frame, in scene space: the road's centre line where the camera sits and where
    /// it looks (each with the road's orientation there), and how the car is doing. Built fresh from the road each frame, so
    /// nothing about where the camera should be depends on how long the frames were.
    /// </summary>
    public readonly struct ChaseTarget
    {
        public readonly Vector3 RearPosition;
        public readonly Quaternion RearRotation;
        public readonly Vector3 AheadPosition;
        public readonly Quaternion AheadRotation;

        /// <summary>Where the car is (on the road surface, at its sideways position).</summary>
        public readonly Vector3 CarPosition;

        /// <summary>The car's position across the road, in metres right of the centre line.</summary>
        public readonly float CarX;

        /// <summary>The car's speed along the road, in metres per second.</summary>
        public readonly float Speed;

        public ChaseTarget(Vector3 rearPosition, Quaternion rearRotation, Vector3 aheadPosition, Quaternion aheadRotation, Vector3 carPosition, float carX, float speed)
        {
            RearPosition = rearPosition;
            RearRotation = rearRotation;
            AheadPosition = aheadPosition;
            AheadRotation = aheadRotation;
            CarPosition = carPosition;
            CarX = carX;
            Speed = speed;
        }

        /// <summary>The target for a car at <paramref name="distance"/> along <paramref name="path"/>, in a scene whose origin is <paramref name="origin"/>.</summary>
        public static ChaseTarget At(IRoadPath path, in WorldPoint origin, double distance, float carX, float speed, ChaseCameraSettings settings)
        {
            RoadSample rear = RoadSampler.At(path, distance - settings.Distance);
            RoadSample ahead = RoadSampler.At(path, distance + settings.LookAhead);
            RoadSample car = RoadSampler.At(path, distance, carX);
            return new ChaseTarget(rear.Position.RelativeTo(origin), rear.Rotation, ahead.Position.RelativeTo(origin), ahead.Rotation, car.Position.RelativeTo(origin), carX, speed);
        }
    }
}
