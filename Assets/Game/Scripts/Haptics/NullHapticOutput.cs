using AF.Core;

namespace WrongLine.Haptics
{
    /// <summary>The output where there is no vibrator (the Editor, desktop): the pulse is only logged.</summary>
    public sealed class NullHapticOutput : IHapticOutput
    {
        public void Vibrate(int milliseconds, int amplitude) => Log.Info($"Haptic {milliseconds} ms, amplitude {amplitude}.");
    }
}
