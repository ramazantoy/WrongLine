namespace WrongLine.Road
{
    /// <summary>
    /// The cross-section every road piece shares (metres, in the piece's local right/up axes, centred on the centre line).
    /// This is the contract the real art in M1b follows: pivot at the entry frame, +Z forward, +Y up.
    /// UV: u runs 0-1 across the asphalt (curbs continue past 0 and 1), v is the distance along the piece divided by
    /// <see cref="TileLength"/>.
    /// </summary>
    public static class RoadSection
    {
        /// <summary>Half the asphalt width: three 3 m lanes.</summary>
        public const float HalfWidth = 4.5f;

        public const float CurbWidth = 0.6f;
        public const float CurbHeight = 0.25f;

        /// <summary>How far the body hangs below the asphalt surface.</summary>
        public const float Thickness = 1f;

        public const float LineWidth = 0.15f;

        /// <summary>Distance of each lane line from the centre line.</summary>
        public const float LineOffset = 1.5f;

        /// <summary>Height of the lane lines above the asphalt, so they do not z-fight with it.</summary>
        public const float LineHeight = 0.02f;

        /// <summary>Distance along the piece that one repeat of the road texture covers.</summary>
        public const float TileLength = 9f;
    }
}
