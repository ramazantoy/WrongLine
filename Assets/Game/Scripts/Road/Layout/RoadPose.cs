using System;
using UnityEngine;

namespace WrongLine.Road
{
    /// <summary>
    /// Where a piece joint sits in the road's world space. Every piece starts and ends level, upright and straight, so a joint
    /// is fully described by a position and a heading (yaw). Double precision: the road is endless.
    /// Yaw 0 faces +Z and positive yaw turns right (towards +X), as in <see cref="TrackRecipe"/>.
    /// </summary>
    public readonly struct RoadPose
    {
        /// <summary>The start of the road: the origin, facing +Z.</summary>
        public static readonly RoadPose Origin = default;

        public readonly double X;
        public readonly double Y;
        public readonly double Z;

        /// <summary>Heading in radians.</summary>
        public readonly double Yaw;

        public RoadPose(double x, double y, double z, double yaw)
        {
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
        }

        public double YawDegrees => Yaw * (180.0 / Math.PI);

        public WorldPoint Position => new WorldPoint(X, Y, Z);

        /// <summary>A point given in a piece's local space (the piece's entry sits at this pose), in world space.</summary>
        public WorldPoint TransformPoint(Vector3 local)
        {
            double sin = Math.Sin(Yaw);
            double cos = Math.Cos(Yaw);
            return new WorldPoint(
                X + local.x * cos + local.z * sin,
                Y + local.y,
                Z - local.x * sin + local.z * cos);
        }

        /// <summary>A direction given in a piece's local space, in world space.</summary>
        public Vector3 TransformDirection(Vector3 local)
        {
            double sin = Math.Sin(Yaw);
            double cos = Math.Cos(Yaw);
            return new Vector3((float)(local.x * cos + local.z * sin), local.y, (float)(-local.x * sin + local.z * cos));
        }

        /// <summary>The pose at the far end of <paramref name="piece"/> when its entry sits at this pose.</summary>
        public RoadPose After(RoadPieceInfo piece)
        {
            WorldPoint exit = TransformPoint(piece.ExitOffset);
            return new RoadPose(exit.X, exit.Y, exit.Z, Yaw + piece.ExitYaw);
        }
    }
}
