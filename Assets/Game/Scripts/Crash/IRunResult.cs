namespace WrongLine.Crash
{
    /// <summary>How the run that just ended went; zeroes until the crash.</summary>
    public interface IRunResult
    {
        /// <summary>The run's distance in whole metres.</summary>
        int Meters { get; }

        /// <summary>The longest run, this one included.</summary>
        int Best { get; }

        /// <summary>True when this run beat the best that stood before it.</summary>
        bool IsRecord { get; }
    }
}
