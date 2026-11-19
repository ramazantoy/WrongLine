using WrongLine.Haptics;

namespace WrongLine.Audio
{
    /// <summary>What an interface touch feels and sounds like.</summary>
    public enum UiFeedbackKind
    {
        /// <summary>A button pressed.</summary>
        Tap,

        /// <summary>A step back: closing a screen, going to the menu.</summary>
        Back,

        /// <summary>A slider or a language step moving.</summary>
        Tick,

        /// <summary>A switch flipped.</summary>
        Toggle,
    }

    /// <summary>The vibration that goes with each <see cref="UiFeedbackKind"/>.</summary>
    public static class UiFeedbackMap
    {
        public static HapticKind HapticOf(UiFeedbackKind kind)
        {
            return kind == UiFeedbackKind.Tick ? HapticKind.Tick : HapticKind.Tap;
        }
    }
}
