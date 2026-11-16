using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WrongLine.Road
{
    /// <summary>
    /// One run along the road, without any scene object: the chain of pieces, the cursor travelling on it, the window of
    /// pieces that should exist and the floating origin. Each <see cref="Advance"/> moves the cursor and reports what the
    /// view must do: which pieces to bring in or let go, and whether the origin moved (then everything is placed again).
    /// </summary>
    public sealed class RoadRun
    {
        private readonly RoadLayout _layout;
        private readonly RoadWindow _window;
        private readonly OriginShift _originShift;
        private readonly List<int> _spawned = new List<int>();
        private readonly List<int> _despawned = new List<int>();

        public RoadRun(IReadOnlyList<RoadPieceInfo> pieces, RoadGeneratorSettings settings, ulong seed, double aheadMeters, double behindMeters, double originShiftMeters)
        {
            RoadPieceInfo start = pieces.FirstOrDefault(piece => piece.IsStart) ?? throw new ArgumentException("There is no start piece.", nameof(pieces));
            _layout = new RoadLayout(start, new RoadGenerator(pieces, settings, seed));
            Cursor = new RoadCursor(_layout);
            _window = new RoadWindow(_layout, aheadMeters, behindMeters);
            _originShift = new OriginShift(originShiftMeters);
            Seed = seed;
        }

        public ulong Seed { get; }

        public IRoadPath Path => _layout;

        public RoadCursor Cursor { get; }

        /// <summary>The pieces that entered and left the window in the last <see cref="Advance"/>, ascending.</summary>
        public IReadOnlyList<int> Spawned => _spawned;

        public IReadOnlyList<int> Despawned => _despawned;

        /// <summary>The pieces now in the window.</summary>
        public int FirstActive => _window.First;

        public int LastActive => _window.Last;

        public int ActiveCount => _window.Count;

        /// <summary>Scene positions are world positions minus this.</summary>
        public WorldPoint Origin => _originShift.Origin;

        /// <summary>Whether the last <see cref="Advance"/> moved the origin, and by how much (the scene positions decreased by it).</summary>
        public bool OriginShifted { get; private set; }

        public Vector3 OriginShiftVector { get; private set; }

        public int ShiftCount { get; private set; }

        /// <summary>Moves the cursor forward (0 is fine and sets the run up the first time) and updates the window and the origin.</summary>
        public void Advance(double meters)
        {
            Cursor.Advance(meters);
            _window.Update(Cursor, _spawned, _despawned);
            _layout.DiscardBefore(_window.First);

            OriginShifted = _originShift.Update(Cursor.WorldPosition, out Vector3 shift);
            OriginShiftVector = shift;
            if (OriginShifted)
            {
                ShiftCount++;
            }
        }
    }
}
