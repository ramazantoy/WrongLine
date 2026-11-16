using System;
using UnityEngine;

namespace WrongLine.Road
{
    /// <summary>
    /// A point travelling along the road: which piece and how far into it. The position is never computed from the total
    /// distance (that would lose precision on a long run); it is the piece's own frame table at the local distance, mapped
    /// through the piece's exact double-precision entry pose.
    /// </summary>
    public sealed class RoadCursor
    {
        private readonly IRoadPath _path;

        public RoadCursor(IRoadPath path)
        {
            _path = path;
            PieceIndex = path.FirstIndex;
        }

        public int PieceIndex { get; private set; }

        /// <summary>Distance into the current piece, in metres.</summary>
        public double LocalS { get; private set; }

        public PlacedPiece Piece => _path[PieceIndex];

        /// <summary>Total distance along the road, in metres.</summary>
        public double Distance => Piece.EntryDistance + LocalS;

        /// <summary>The frame in the current piece's local space.</summary>
        public TrackFrame LocalFrame => Piece.Piece.Shape.Evaluate((float)LocalS);

        public WorldPoint WorldPosition => Piece.Entry.TransformPoint(LocalFrame.Position);

        /// <summary>The road's orientation here: forward along the road, up as banked.</summary>
        public Quaternion WorldRotation => Quaternion.Euler(0f, (float)Piece.Entry.YawDegrees, 0f) * LocalFrame.Rotation;

        /// <summary>Moves forward, crossing piece joints and placing more road at the far end when it runs out.</summary>
        public void Advance(double meters)
        {
            if (!(meters >= 0.0))
            {
                throw new ArgumentOutOfRangeException(nameof(meters), "The cursor only moves forward.");
            }

            double remaining = meters;
            while (true)
            {
                double room = _path[PieceIndex].Piece.Length - LocalS;
                if (remaining < room)
                {
                    LocalS += remaining;
                    return;
                }

                remaining -= room;
                PieceIndex++;
                LocalS = 0.0;
                if (PieceIndex >= _path.EndIndex)
                {
                    _path.EnsureCovered(_path.EndDistance + 1.0);
                }
            }
        }
    }
}
