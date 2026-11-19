namespace WrongLine.UI
{
    /// <summary>The player's longest run, kept across runs and devices.</summary>
    public interface IBestDistance
    {
        /// <summary>The longest run so far, in whole metres; 0 before the first run.</summary>
        int Meters { get; }

        /// <summary>Records a finished run. Returns true when it beat the best (the first run always does, if it went anywhere).</summary>
        bool Submit(double meters);
    }
}
