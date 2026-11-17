namespace WrongLine.Scenery
{
    /// <summary>How a prop is spread along the road.</summary>
    public enum PropMode
    {
        /// <summary>Random places: on average <see cref="PropRule.Density"/> per 100 m of road, both sides together.</summary>
        Scatter,

        /// <summary>One every <see cref="PropRule.Spacing"/> metres of road at a fixed distance from it (street lamps, snow poles).</summary>
        Row,
    }

    /// <summary>Which side of the road a <see cref="PropMode.Row"/> prop stands on.</summary>
    public enum RowSide
    {
        Alternate,
        Right,
        Left,
    }

    /// <summary>Where one kind of prop may stand, as the planner reads it. Built from the scenery catalog.</summary>
    public sealed class PropRule
    {
        public PropRule(int id, int biome, PropMode mode, float density, float spacing, RowSide side, float lateralMin, float lateralMax, float footprint, float scaleMin, float scaleMax)
        {
            Id = id;
            Biome = biome;
            Mode = mode;
            Density = density;
            Spacing = spacing;
            Side = side;
            LateralMin = lateralMin;
            LateralMax = lateralMax;
            Footprint = footprint;
            ScaleMin = scaleMin;
            ScaleMax = scaleMax;
        }

        /// <summary>Index of the prop in the scenery set (and of its pool).</summary>
        public int Id { get; }

        /// <summary>The biome the prop belongs to, or <see cref="Shared"/> for one that stands in every biome.</summary>
        public int Biome { get; }

        public const int Shared = -1;

        public PropMode Mode { get; }

        public float Density { get; }

        public float Spacing { get; }

        public RowSide Side { get; }

        /// <summary>Distance from the centre line, in metres (a row uses the minimum).</summary>
        public float LateralMin { get; }

        public float LateralMax { get; }

        /// <summary>Radius the prop needs on the ground, in metres: kept clear of the road and of other props.</summary>
        public float Footprint { get; }

        public float ScaleMin { get; }

        public float ScaleMax { get; }
    }
}
