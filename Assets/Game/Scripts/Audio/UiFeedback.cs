using AF.Audio;
using WrongLine.Haptics;

namespace WrongLine.Audio
{
    /// <summary>
    /// Plays an interface touch's click through the root's sounds (so it is not cut by the scene change a button starts) and its
    /// vibration through the haptics, which honour the player's setting.
    /// </summary>
    internal sealed class UiFeedback : IUiFeedback
    {
        private readonly IGlobalSounds _sounds;
        private readonly IHaptics _haptics;
        private readonly GameAudioConfig _config;

        public UiFeedback(IGlobalSounds sounds, IHaptics haptics, GameAudioConfig config)
        {
            _sounds = sounds;
            _haptics = haptics;
            _config = config;
        }

        public void Play(UiFeedbackKind kind)
        {
            _sounds.Play(_config.CueOf(kind));
            _haptics.Play(UiFeedbackMap.HapticOf(kind));
        }
    }
}
