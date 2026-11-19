using AF.Audio;
using VContainer.Unity;

namespace WrongLine.Audio
{
    /// <summary>Starts the menu's theme when the menu scene starts; the music crossfades from whatever played before.</summary>
    internal sealed class MenuMusic : IStartable
    {
        private readonly IMusic _music;
        private readonly GameAudioConfig _config;

        public MenuMusic(IMusic music, GameAudioConfig config)
        {
            _music = music;
            _config = config;
        }

        public void Start() => _music.Play(_config.MenuTheme, _config.MenuFadeSeconds);
    }

    /// <summary>Starts the driving theme when the driving scene starts. A crash stops the music, so a retry starts it again.</summary>
    internal sealed class DriveMusic : IStartable
    {
        private readonly IMusic _music;
        private readonly GameAudioConfig _config;

        public DriveMusic(IMusic music, GameAudioConfig config)
        {
            _music = music;
            _config = config;
        }

        public void Start() => _music.Play(_config.DriveTheme, _config.DriveFadeSeconds);
    }
}
