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
        /// <summary>Number of lanes across the asphalt.</summary>
        public const int LaneCount = 4;

        /// <summary>Width of one lane.</summary>
        public const float LaneWidth = 3f;

        /// <summary>Number of lane lines between the lanes (the outer edges have none).</summary>
        public const int LineCount = LaneCount - 1;

        /// <summary>Half the asphalt width: four 3 m lanes.</summary>
        public const float HalfWidth = LaneCount * LaneWidth * 0.5f;

        public const float CurbWidth = 0.6f;
        public const float CurbHeight = 0.25f;

        /// <summary>How far the body hangs below the asphalt surface.</summary>
        public const float Thickness = 1f;

        public const float LineWidth = 0.15f;

        /// <summary>
        /// The road is two lanes each way: the left half is the oncoming side, the right half goes the way the car does.
        /// (The lane count must be even.)
        /// </summary>
        public const int OncomingLaneCount = LaneCount / 2;

        /// <summary>
        /// The line between the two directions (an index for <see cref="LineX"/>): solid yellow. The other lines only
        /// separate lanes of one direction and are dashed white.
        /// </summary>
        public const int CenterLineIndex = LineCount / 2;

        /// <summary>Length of one painted dash of a lane line, out of <see cref="LineDashPeriod"/>.</summary>
        public const float LineDashLength = 3f;

        /// <summary>Sideways position of lane line <paramref name="index"/> (0 .. <see cref="LineCount"/> - 1, left to right) from the centre line.</summary>
        public static float LineX(int index)
        {
            return (index + 1 - LaneCount * 0.5f) * LaneWidth;
        }

        /// <summary>Sideways position of the middle of lane <paramref name="lane"/> (0 is the leftmost).</summary>
        public static float LaneCenterX(int lane)
        {
            return (lane + 0.5f - LaneCount * 0.5f) * LaneWidth;
        }

        /// <summary>Height of the lane lines above the asphalt, so they do not z-fight with it.</summary>
        public const float LineHeight = 0.02f;

        /// <summary>Distance along the piece that one repeat of the road texture covers.</summary>
        public const float TileLength = 9f;

        /// <summary>
        /// Distance along the road of one dash and one gap of the lane lines. The line material's texture is tiled to this
        /// (the mesh UV stays at <see cref="TileLength"/>), and every piece is a whole number of periods long, so the dashes
        /// run on across a joint without a long gap or a double dash.
        /// </summary>
        public const float LineDashPeriod = 10f;
    }
}
