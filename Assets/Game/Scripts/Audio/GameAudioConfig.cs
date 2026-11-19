using AF.Audio;
using AF.Core;
using UnityEngine;

namespace WrongLine.Audio
{
    /// <summary>The game-wide sounds: the interface's clicks, the game over jingles and the two music themes.</summary>
    [CreateAssetMenu(menuName = "WrongLine/Game Audio Config")]
    public sealed class GameAudioConfig : ScriptableObject
    {
        [Header("Interface")]
        [SerializeField, Required] private SoundCue _uiTap;
        [SerializeField, Required] private SoundCue _uiBack;
        [SerializeField, Required] private SoundCue _uiTick;
        [SerializeField, Required] private SoundCue _uiToggle;

        [Header("Game over")]
        [SerializeField, Required] private SoundCue _gameOverJingle;
        [SerializeField, Required] private SoundCue _recordJingle;

        [Header("Music")]
        [SerializeField, Required] private AudioClip _menuTheme;
        [SerializeField, Required] private AudioClip _driveTheme;
        [SerializeField, Min(0f)] private float _menuFadeSeconds = 1.5f;
        [SerializeField, Min(0f)] private float _driveFadeSeconds = 2f;

        public SoundCue GameOverJingle => _gameOverJingle;
        public SoundCue RecordJingle => _recordJingle;
        public AudioClip MenuTheme => _menuTheme;
        public AudioClip DriveTheme => _driveTheme;
        public float MenuFadeSeconds => _menuFadeSeconds;
        public float DriveFadeSeconds => _driveFadeSeconds;

        public SoundCue CueOf(UiFeedbackKind kind)
        {
            switch (kind)
            {
                case UiFeedbackKind.Back:
                    return _uiBack;
                case UiFeedbackKind.Tick:
                    return _uiTick;
                case UiFeedbackKind.Toggle:
                    return _uiToggle;
                default:
                    return _uiTap;
            }
        }
    }
}
