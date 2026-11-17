namespace WrongLine.World
{
    /// <summary>The time of day as the other systems read it.</summary>
    internal interface IDayNight
    {
        /// <summary>0 in full day, 1 in full night (see <see cref="DayNightSettings.Darkness"/>).</summary>
        float Darkness { get; }
    }
}
