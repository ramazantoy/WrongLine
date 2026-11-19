namespace WrongLine.Haptics
{
    /// <summary>What actually vibrates: the phone's vibrator on Android, nothing elsewhere.</summary>
    public interface IHapticOutput
    {
        /// <summary>One pulse. <paramref name="amplitude"/> is 1 to 255 (where the device supports it).</summary>
        void Vibrate(int milliseconds, int amplitude);
    }
}
