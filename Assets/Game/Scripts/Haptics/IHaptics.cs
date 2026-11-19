namespace WrongLine.Haptics
{
    /// <summary>The phone's vibration, for crashes and taps. Respects the player's haptics setting.</summary>
    public interface IHaptics
    {
        void Play(HapticKind kind);
    }
}
