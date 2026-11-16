using System;

namespace WrongLine.Road
{
    /// <summary>What a road piece is, derived from its recipe. The generator picks pieces by these.</summary>
    [Flags]
    public enum RoadTags
    {
        None = 0,
        Straight = 1,
        Curve = 2,
        SCurve = 4,
        Climb = 8,
        Drop = 16,
        Hill = 32,
        Dip = 64,
        Banked = 128,
    }
}
