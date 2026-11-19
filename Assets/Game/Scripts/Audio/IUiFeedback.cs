namespace WrongLine.Audio
{
    /// <summary>The click and the vibration of an interface touch. Respects the volume and the haptics settings; plays while paused and over a scene change.</summary>
    public interface IUiFeedback
    {
        void Play(UiFeedbackKind kind);
    }
}
