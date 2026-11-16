using System;
using UnityEngine;

namespace WrongLine.Road
{
    /// <summary>Where a point on the road is and how the road is oriented there (forward along the road, up as banked).</summary>
    public readonly struct RoadSample
    {
        public readonly WorldPoint Position;
        public readonly Quaternion Rotation;

        public RoadSample(in WorldPoint position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }

    /// <summary>
    /// Reads the road at any distance along it, and a little to the side of or above the centre line, without a cursor:
    /// the camera and the car both need points other than "where the cursor is". Pure; changes nothing in the path.
    /// Behind the oldest piece the road runs straight on (the first piece is the straight start); past the newest it stops.
    /// </summary>
    public static class RoadSampler
    {
        /// <param name="distance">Distance along the road, in metres.</param>
        /// <param name="lateral">Metres to the right of the centre line, along the (banked) road surface.</param>
        /// <param name="height">Metres above the road surface, along the road's up.</param>
        public static RoadSample At(IRoadPath path, double distance, float lateral = 0f, float height = 0f)
        {
            int index = Find(path, distance);
            PlacedPiece placed = path[index];

            double local = distance - placed.EntryDistance;
            TrackFrame frame = placed.Piece.Shape.Evaluate((float)Math.Max(0.0, local));
            Vector3 position = frame.Position + frame.Rotation * new Vector3(lateral, height, 0f);
            if (local < 0.0)
            {
                position += frame.Forward * (float)local; // Behind the oldest piece: straight on backwards.
            }

            Quaternion rotation = Quaternion.Euler(0f, (float)placed.Entry.YawDegrees, 0f) * frame.Rotation;
            return new RoadSample(placed.Entry.TransformPoint(position), rotation);
        }

        /// <summary>The piece that covers <paramref name="distance"/>: the oldest for anything behind it, the newest for anything past it.</summary>
        private static int Find(IRoadPath path, double distance)
        {
            int low = path.FirstIndex;
            int high = path.EndIndex - 1;
            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                if (path[middle].EntryDistance <= distance)
                {
                    low = middle;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return low;
        }
    }
}
