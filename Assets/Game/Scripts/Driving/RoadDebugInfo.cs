using System.Globalization;

namespace WrongLine.Driving
{
    /// <summary>What the debug HUD shows about the run.</summary>
    public readonly struct RoadDebugInfo
    {
        public readonly double Distance;
        public readonly int PieceIndex;
        public readonly string PieceName;
        public readonly double LocalS;
        public readonly float PieceLength;
        public readonly ulong Seed;
        public readonly float Speed;
        public readonly int ActivePieces;
        public readonly int Shifts;
        public readonly float CarX;
        public readonly int Lane;
        public readonly string Signal;

        public RoadDebugInfo(double distance, int pieceIndex, string pieceName, double localS, float pieceLength, ulong seed, float speed, int activePieces, int shifts, float carX, int lane, string signal)
        {
            Distance = distance;
            PieceIndex = pieceIndex;
            PieceName = pieceName;
            LocalS = localS;
            PieceLength = pieceLength;
            Seed = seed;
            Speed = speed;
            ActivePieces = activePieces;
            Shifts = shifts;
            CarX = carX;
            Lane = lane;
            Signal = signal;
        }

        /// <summary>Invariant culture: a Turkish device would otherwise print "12,5".</summary>
        public string Format(float framesPerSecond)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "s {0:F1} m   piece #{1} {2} ({3:F1}/{4:F0} m)\nseed {5}   speed {6:F1} m/s\nx {10:F2} m   lane {11}   signal {12}\nactive {7}   origin shifts {8}   {9:F0} fps",
                Distance, PieceIndex, PieceName, LocalS, PieceLength, Seed, Speed, ActivePieces, Shifts, framesPerSecond, CarX, Lane, Signal);
        }
    }
}
