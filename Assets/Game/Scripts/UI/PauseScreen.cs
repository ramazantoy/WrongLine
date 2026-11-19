using System;
using AF.Core;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using WrongLine.Crash;
using WrongLine.Haptics;

namespace WrongLine.UI
{
    /// <summary>
    /// The pause menu: Resume, Settings (the shared settings screen; this one steps back while it is up, as the main menu does) and
    /// Menu. While it is open it holds a pause request, so the game clock stands still; resuming or leaving lets go of it.
    /// </summary>
    [RequireComponent(typeof(UiScreen))]
    public sealed class PauseScreen : MonoBehaviour
    {
        private const string PAUSE_REASON = "pause-menu";

        [SerializeField, Required] private Button _resume;
        [SerializeField, Required] private Button _settings;
        [SerializeField, Required] private Button _menu;

        private UiScreen _screen;
        private IPauseService _pause;
        private SettingsScreen _settingsScreen;
        private IHaptics _haptics;
        private GameOverActions _actions;
        private IDisposable _handle;

        [Inject]
        public void Construct(IPauseService pause, ISceneLoader scenes, SettingsScreen settingsScreen, IHaptics haptics)
        {
            _pause = pause;
            _actions = new GameOverActions(scenes);
            _settingsScreen = settingsScreen;
            _haptics = haptics;
        }

        /// <summary>True from <see cref="Open"/> until <see cref="Resume"/> (or leaving for the menu), also while the settings are up over it.</summary>
        public bool IsOpen => _handle != null;

        // Not set in Awake: the screen starts inactive and is shown before Unity has run it.
        private UiScreen Screen => _screen != null ? _screen : (_screen = GetComponent<UiScreen>());

        private void Start() => _settingsScreen.Closed += OnSettingsClosed;

        private void OnEnable()
        {
            _resume.onClick.AddListener(OnResume);
            _settings.onClick.AddListener(OnSettings);
            _menu.onClick.AddListener(OnMenu);
        }

        private void OnDisable()
        {
            _resume.onClick.RemoveListener(OnResume);
            _settings.onClick.RemoveListener(OnSettings);
            _menu.onClick.RemoveListener(OnMenu);
        }

        private void OnDestroy()
        {
            _handle?.Dispose();
            if (_settingsScreen != null)
            {
                _settingsScreen.Closed -= OnSettingsClosed;
            }
        }

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            _handle = _pause.Request(PAUSE_REASON);
            Screen.Show();
        }

        public void Resume()
        {
            if (!IsOpen)
            {
                return;
            }

            _handle.Dispose();
            _handle = null;
            Screen.Hide();
        }

        private void OnResume()
        {
            _haptics.Play(HapticKind.Tap);
            Resume();
        }

        private void OnSettings()
        {
            _haptics.Play(HapticKind.Tap);
            _settingsScreen.Show();
            Screen.Hide();
        }

        private void OnSettingsClosed()
        {
            // Closed also when the scene is torn down or the pause was left for the menu: only a pause that is still open comes back.
            if (IsOpen)
            {
                Screen.Show();
            }
        }

        private void OnMenu()
        {
            _haptics.Play(HapticKind.Tap);
            // The pause is let go first: the scene change must not start with the game stopped.
            _handle?.Dispose();
            _handle = null;
            _actions.Menu();
        }
    }
}
