using System;
using System.Collections.Generic;

namespace WrongLine.Road
{
    /// <summary>
    /// The chain of placed pieces. The start piece sits at the origin and is outside the generator's rules; the generator
    /// then continues from its exit, one piece at a time.
    /// </summary>
    public sealed class RoadLayout : IRoadPath
    {
        private readonly List<PlacedPiece> _placed = new List<PlacedPiece>();
        private readonly RoadGenerator _generator;
        private int _first;

        public RoadLayout(RoadPieceInfo start, RoadGenerator generator)
        {
            _generator = generator;
            _placed.Add(new PlacedPiece(0, start, RoadPose.Origin, 0.0));
        }

        public ulong Seed => _generator.Seed;

        public int FirstIndex => _first;

        public int EndIndex => _first + _placed.Count;

        public double EndDistance => _placed[_placed.Count - 1].ExitDistance;

        public PlacedPiece this[int index]
        {
            get
            {
                if (index < _first || index >= EndIndex)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), $"Piece {index} is not kept (kept: {_first} to {EndIndex - 1}).");
                }

                return _placed[index - _first];
            }
        }

        public void EnsureCovered(double distance)
        {
            while (EndDistance < distance)
            {
                PlacedPiece last = _placed[_placed.Count - 1];
                RoadPieceInfo next = _generator.Next(last.Exit, last.ExitDistance);
                _placed.Add(new PlacedPiece(last.Index + 1, next, last.Exit, last.ExitDistance));
            }
        }

        public void DiscardBefore(int index)
        {
            index = Math.Min(index, EndIndex - 1);
            if (index <= _first)
            {
                return;
            }

            _placed.RemoveRange(0, index - _first);
            _first = index;
        }
    }
}
