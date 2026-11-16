namespace WrongLine.Road
{
    /// <summary>
    /// The endless chain of road pieces. Pieces are numbered from the start piece (0) and added at the far end on demand;
    /// old ones can be dropped from the near end. Everything that moves along or draws the road reads it through this.
    /// </summary>
    public interface IRoadPath
    {
        /// <summary>The oldest piece still kept.</summary>
        int FirstIndex { get; }

        /// <summary>One past the newest piece placed so far.</summary>
        int EndIndex { get; }

        /// <summary>Distance along the road at the far end of the newest piece, in metres.</summary>
        double EndDistance { get; }

        /// <exception cref="System.ArgumentOutOfRangeException">The piece was dropped or is not placed yet.</exception>
        PlacedPiece this[int index] { get; }

        /// <summary>Places pieces until the road reaches at least <paramref name="distance"/>.</summary>
        void EnsureCovered(double distance);

        /// <summary>Drops the pieces before <paramref name="index"/>. The newest piece is always kept.</summary>
        void DiscardBefore(int index);
    }
}
