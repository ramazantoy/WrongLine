using System.Collections.Generic;

namespace WrongLine.Scenery
{
    /// <summary>
    /// The road pieces that have their props out: one contiguous run of indices. Told the run that should have them, it
    /// reports which pieces gained their props and which lost them since the last time.
    /// </summary>
    public sealed class SceneryWindow
    {
        private int _first;
        private int _end;

        /// <summary>The first piece with props.</summary>
        public int First => _first;

        /// <summary>One past the last piece with props.</summary>
        public int End => _end;

        public int Count => _end - _first;

        /// <summary>
        /// Moves the window to the pieces <paramref name="first"/> to <paramref name="end"/> (exclusive; empty when
        /// <paramref name="end"/> is not past <paramref name="first"/>). Both lists are cleared and filled in ascending order.
        /// </summary>
        public void Update(int first, int end, List<int> added, List<int> removed)
        {
            added.Clear();
            removed.Clear();
            if (end < first)
            {
                end = first;
            }

            for (int index = _first; index < _end; index++)
            {
                if (index < first || index >= end)
                {
                    removed.Add(index);
                }
            }

            for (int index = first; index < end; index++)
            {
                if (index < _first || index >= _end)
                {
                    added.Add(index);
                }
            }

            _first = first;
            _end = end;
        }
    }
}
