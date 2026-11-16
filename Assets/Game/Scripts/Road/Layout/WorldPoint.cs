using UnityEngine;

namespace WrongLine.Road
{
    /// <summary>A position in the road's world space in double precision, so it stays exact however far the road runs.</summary>
    public readonly struct WorldPoint
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;

        public WorldPoint(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double DistanceTo(in WorldPoint other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            double dz = Z - other.Z;
            return System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>The point relative to <paramref name="origin"/>, as a float vector (small once the origin is near).</summary>
        public Vector3 RelativeTo(in WorldPoint origin)
        {
            return new Vector3((float)(X - origin.X), (float)(Y - origin.Y), (float)(Z - origin.Z));
        }

        public override string ToString() => $"({X:F3}, {Y:F3}, {Z:F3})";
    }
}
