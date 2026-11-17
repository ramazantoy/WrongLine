using System;
using UnityEngine;

namespace WrongLine.Scenery
{
    /// <summary>
    /// The ground strip beside the road. Baked into every road piece by WrongLine/Road/Rebuild Pieces: change a number here,
    /// then rebuild the pieces.
    /// </summary>
    [Serializable]
    public sealed class GroundSettings
    {
        [Tooltip("How far below the asphalt the ground meets the side of the road, in metres.")]
        [SerializeField] private float _edgeDrop = 0.3f;

        [Tooltip("How far the ground reaches out from the edge of the curb, on each side, in metres.")]
        [SerializeField] private float _width = 80f;

        [Tooltip("On the inside of a bend the ground reaches this share of the bend's radius: a little past the centre (above 1) closes the inner corner; the ground may fold there, which the ground shader draws as flat.")]
        [SerializeField, Range(0.1f, 1.5f)] private float _innerReachRatio = 1.1f;

        [Tooltip("The tightest bend within this distance before and after a point limits the ground there, in metres.")]
        [SerializeField] private float _curvatureWindow = 4f;

        [Tooltip("Distance between two cross-sections of the ground mesh along the road, in metres. The world bend needs vertices to bend.")]
        [SerializeField] private float _ringSpacing = 2f;

        public GroundSettings()
        {
        }

        public GroundSettings(float edgeDrop, float width, float innerReachRatio, float curvatureWindow, float ringSpacing)
        {
            _edgeDrop = edgeDrop;
            _width = width;
            _innerReachRatio = innerReachRatio;
            _curvatureWindow = curvatureWindow;
            _ringSpacing = ringSpacing;
        }

        public float EdgeDrop => _edgeDrop;

        public float Width => _width;

        public float InnerReachRatio => _innerReachRatio;

        public float CurvatureWindow => _curvatureWindow;

        public float RingSpacing => _ringSpacing;

        /// <exception cref="ArgumentException">A number makes no sense.</exception>
        public void Validate()
        {
            if (!(_edgeDrop >= 0f) || !(_width > 0f) || !(_curvatureWindow >= 0f) || !(_ringSpacing > 0f))
            {
                throw new ArgumentException("EdgeDrop and CurvatureWindow must not be negative; Width and RingSpacing must be positive.");
            }

            if (!(_innerReachRatio > 0f) || _innerReachRatio > 1.5f)
            {
                throw new ArgumentException("InnerReachRatio must be above 0 and at most 1.5.");
            }
        }
    }
}
