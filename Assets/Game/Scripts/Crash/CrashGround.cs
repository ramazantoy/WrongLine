using UnityEngine;
using WrongLine.Road;

namespace WrongLine.Crash
{
    /// <summary>
    /// The solid road the wreck's parts land on. The road has no colliders (the car is moved by road-space rules, not physics), so
    /// at a crash a row of thin boxes is laid along the road around the crash, each turned to the road's own frame there.
    /// </summary>
    internal static class CrashGround
    {
        private const float Thickness = 0.5f;

        /// <summary>The ground's width: the road and a margin for what flies off the side.</summary>
        private const float Margin = 6f;

        /// <summary>Boxes slightly overlap, so a part cannot fall through a seam.</summary>
        private const float Overlap = 1.1f;

        /// <returns>The object holding the boxes; destroy it when the run ends.</returns>
        public static GameObject Build(IRoadPath path, in WorldPoint origin, double center, CrashSettings.WreckGroup settings)
        {
            var root = new GameObject("CrashGround");
            float length = settings.GroundSegmentLength;
            float width = RoadSection.HalfWidth * 2f + Margin * 2f;
            for (int i = 0; i < settings.GroundSegments; i++)
            {
                double distance = center + (i - (settings.GroundSegments - 1) * 0.5) * length;
                RoadSample sample = RoadSampler.At(path, distance);
                Vector3 up = sample.Rotation * Vector3.up;

                var segment = new GameObject("Segment " + i);
                segment.transform.SetParent(root.transform, false);
                segment.transform.SetPositionAndRotation(sample.Position.RelativeTo(origin) - up * (Thickness * 0.5f), sample.Rotation);
                var box = segment.AddComponent<BoxCollider>();
                box.size = new Vector3(width, Thickness, length * Overlap);
            }

            return root;
        }
    }
}
