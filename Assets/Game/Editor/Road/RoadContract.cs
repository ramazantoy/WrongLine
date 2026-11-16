using System;
using UnityEngine;

namespace WrongLine.Road.Editor
{
    /// <summary>
    /// What the real road art (made in Blender) reads: one piece's recipe and its baked centre line. Unity makes this, the
    /// artist's tool only reads it. Plain flat arrays and camelCase names, because <see cref="JsonUtility"/> writes it and
    /// the readers are not Unity. Unity space: left-handed, +Y up, +Z forward, +X right.
    /// </summary>
    [Serializable]
    public sealed class RoadPieceContract
    {
        public string name;
        public bool isStart;
        public string tags;
        public float length;
        public float yawDegrees;
        public string yawProfile;
        public float pitchDegrees;
        public string pitchProfile;
        public float bankDegrees;

        /// <summary>Distance along the piece between two frames, in metres.</summary>
        public float frameStep;

        public int frameCount;

        /// <summary>The centre line, x y z per frame, flattened. The first frame is the piece's pivot (origin).</summary>
        public float[] positions;

        /// <summary>The direction of travel per frame, x y z, flattened.</summary>
        public float[] forwards;

        /// <summary>The road's up per frame (banking included), x y z, flattened.</summary>
        public float[] ups;

        public float[] exitPosition;

        /// <summary>How far the heading has turned at the exit, in degrees (positive turns right).</summary>
        public float exitYawDegrees;
    }

    /// <summary>The cross-section and conventions every piece shares.</summary>
    [Serializable]
    public sealed class RoadSectionContract
    {
        public string[] notes;
        public float halfWidth;
        public float curbWidth;
        public float curbHeight;
        public float thickness;
        public float lineWidth;
        public float lineOffset;
        public float lineHeight;
        public float tileLength;

        /// <summary>Submesh order of a piece's mesh, which is the order of its material slots.</summary>
        public string[] submeshOrder;
    }
}
