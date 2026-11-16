namespace WrongLine.Road
{
    /// <summary>A piece in the chain: which one, where its entry and exit sit in world space, and how far along the road it is.</summary>
    public readonly struct PlacedPiece
    {
        public readonly int Index;
        public readonly RoadPieceInfo Piece;
        public readonly RoadPose Entry;
        public readonly RoadPose Exit;

        /// <summary>Distance along the road at the entry and at the exit, in metres.</summary>
        public readonly double EntryDistance;

        public readonly double ExitDistance;

        public PlacedPiece(int index, RoadPieceInfo piece, in RoadPose entry, double entryDistance)
        {
            Index = index;
            Piece = piece;
            Entry = entry;
            Exit = entry.After(piece);
            EntryDistance = entryDistance;
            ExitDistance = entryDistance + piece.Length;
        }
    }
}
