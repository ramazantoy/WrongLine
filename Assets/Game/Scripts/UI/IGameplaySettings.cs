namespace WrongLine.UI
{
    /// <summary>The game's own settings: what the player can switch that is not display, volume or language.</summary>
    public interface IGameplaySettings
    {
        /// <summary>Whether the phone vibrates for crashes and taps.</summary>
        bool Haptics { get; }

        void SetHaptics(bool on);
    }
}
