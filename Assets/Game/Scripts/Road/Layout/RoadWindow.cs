using System;
using System.Collections.Generic;

namespace WrongLine.Road
{
    /// <summary>
    /// Decides which pieces should exist around the cursor: every piece that reaches from <c>behind</c> metres back to
    /// <c>ahead</c> metres forward. Each update reports which pieces to bring in and which to let go; the pieces are always
    /// one contiguous run of indices.
    /// </summary>
    public sealed class RoadWindow
    {
        private readonly IRoadPath _path;
        private readonly double _ahead;
        private readonly double _behind;

        public RoadWindow(IRoadPath path, double aheadMeters, double behindMeters)
        {
            if (!(aheadMeters > 0.0) || !(behindMeters >= 0.0))
            {
                throw new ArgumentOutOfRangeException(nameof(aheadMeters), "Ahead must be positive and behind not negative.");
            }

            _path = path;
            _ahead = aheadMeters;
            _behind = behindMeters;
            First = 0;
            Last = -1;
        }

        /// <summary>The first and last piece in the window; the window is empty (<c>Last &lt; First</c>) until the first update.</summary>
        public int First { get; private set; }

        public int Last { get; private set; }

        public int Count => Last - First + 1;

        public bool Contains(int index) => index >= First && index <= Last;

        /// <param name="spawned">Cleared and filled with the pieces that entered the window, in ascending order.</param>
        /// <param name="despawned">Cleared and filled with the pieces that left it, in ascending order.</param>
        public void Update(RoadCursor cursor, List<int> spawned, List<int> despawned)
        {
            spawned.Clear();
            despawned.Clear();

            double distance = cursor.Distance;
            _path.EnsureCovered(distance + _ahead);

            int last = cursor.PieceIndex;
            while (_path[last].ExitDistance < distance + _ahead)
            {
                last++;
            }

            int first = cursor.PieceIndex;
            while (first > _path.FirstIndex && _path[first - 1].ExitDistance >= distance - _behind)
            {
                first--;
            }

            for (int i = First; i <= Last; i++)
            {
                if (i < first || i > last)
                {
                    despawned.Add(i);
                }
            }

            for (int i = first; i <= last; i++)
            {
                if (!Contains(i))
                {
                    spawned.Add(i);
                }
            }

            First = first;
            Last = last;
        }
    }
}
