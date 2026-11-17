using System;
using UnityEngine;
using WrongLine.Road;

namespace WrongLine.Scenery
{
    /// <summary>
    /// The shape of the ground beside a road piece, in the piece's local space. The ground starts at the outer edge of the curb
    /// (on the banked road, a little below the asphalt) and runs out level from there: a bank tilts the road, not the land.
    /// The mesh builder and the prop planner both read it, so a prop stands on the ground that is drawn.
    /// </summary>
    public static class GroundProfile
    {
        /// <summary>Distance of the curb's outer edge from the centre line.</summary>
        public const float EdgeX = RoadSection.HalfWidth + RoadSection.CurbWidth;

        /// <summary>
        /// The ground point beside <paramref name="frame"/> at <paramref name="x"/> metres to the right (negative: left).
        /// Anything closer than <see cref="EdgeX"/> gives the edge itself.
        /// </summary>
        public static Vector3 Point(in TrackFrame frame, float x, float edgeDrop)
        {
            float side = x >= 0f ? 1f : -1f;
            Vector3 edge = frame.Position + frame.Right * (side * EdgeX) - frame.Up * edgeDrop;
            float beyond = Math.Max(0f, Math.Abs(x) - EdgeX);
            return edge + FlatRight(frame) * (side * beyond);
        }

        /// <summary>
        /// The ground at <paramref name="x"/> beside the road at local distance <paramref name="s"/>, given how far the ground
        /// reaches there (<see cref="Reach"/>). At the curb it is at the road's height; going out it settles down (or up) to the
        /// straight line between the piece's entry and exit heights, so a hill becomes a ridge rather than a floating terrace,
        /// and neighbouring pieces meet at their joints (every piece starts and ends level).
        /// </summary>
        public static Vector3 Ground(TrackShape shape, float s, float x, float reach, float edgeDrop)
        {
            Vector3 point = Point(shape.Evaluate(s), x, edgeDrop);
            float span = reach - EdgeX;
            float share = span > 1e-4f ? Mathf.Clamp01((Math.Abs(x) - EdgeX) / span) : 0f;
            float settle = share * share * (3f - 2f * share);
            point.y = Mathf.Lerp(point.y, Baseline(shape, s) - edgeDrop, settle);
            return point;
        }

        /// <summary>Height of the straight line between the piece's entry and exit at local distance <paramref name="s"/>.</summary>
        public static float Baseline(TrackShape shape, float s)
        {
            float t = shape.Length > 0f ? Mathf.Clamp01(s / shape.Length) : 0f;
            return Mathf.Lerp(shape.Entry.Position.y, shape.Exit.Position.y, t);
        }

        /// <summary>The road's right with the bank taken out: level, at right angles to the road's heading.</summary>
        public static Vector3 FlatRight(in TrackFrame frame)
        {
            Vector3 forward = frame.Forward;
            var right = new Vector3(forward.z, 0f, -forward.x);
            if (right.sqrMagnitude < 1e-8f)
            {
                // Pointing straight up or down: fall back to the road's own right, levelled.
                right = frame.Right;
                right.y = 0f;
            }

            return right.normalized;
        }

        /// <summary>Heading of the road at local distance <paramref name="s"/>, in radians; positive turns right.</summary>
        public static float Heading(TrackShape shape, float s)
        {
            Vector3 forward = shape.Evaluate(s).Forward;
            return (float)Math.Atan2(forward.x, forward.z);
        }

        /// <summary>How fast the heading turns at <paramref name="s"/>, in radians per metre; positive turns right.</summary>
        public static float Curvature(TrackShape shape, float s)
        {
            float delta = Math.Max(shape.Step, 1f);
            float before = Math.Max(0f, s - delta);
            float after = Math.Min(shape.Length, s + delta);
            if (after <= before)
            {
                return 0f;
            }

            float turn = Heading(shape, after) - Heading(shape, before);
            turn = (float)Math.IEEERemainder(turn, 2.0 * Math.PI); // -pi .. pi
            return turn / (after - before);
        }

        /// <summary>
        /// How far from the centre line the ground reaches on <paramref name="side"/> (+1 right, -1 left) around
        /// <paramref name="s"/>: the full width, or on the inside of a bend a share of its radius (a little past its centre, so the inner corner is covered; the ground may fold there).
        /// Never less than <see cref="EdgeX"/>.
        /// </summary>
        public static float Reach(TrackShape shape, float s, int side, GroundSettings settings)
        {
            float tightest = 0f;
            float step = Math.Max(shape.Step, 0.5f);
            float from = Math.Max(0f, s - settings.CurvatureWindow);
            float to = Math.Min(shape.Length, s + settings.CurvatureWindow);
            for (float t = from; t <= to + 1e-4f; t += step)
            {
                tightest = Math.Max(tightest, Curvature(shape, t) * side);
            }

            float full = EdgeX + settings.Width;
            if (tightest <= 1e-6f)
            {
                return full;
            }

            return Math.Min(full, Math.Max(EdgeX, settings.InnerReachRatio / tightest));
        }
    }
}
