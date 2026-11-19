using System;
using System.Globalization;
using AF.Core;
using AF.Input;
using Cysharp.Threading.Tasks;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;
using WrongLine.Haptics;
using WrongLine.UI;

namespace WrongLine.Menu
{
    /// <summary>
    /// The main menu: Play opens the driving scene, Settings the settings screen, and the best run is shown. Back (the Android back
    /// key) closes what is open, and with nothing open asks whether to quit.
    /// </summary>
    [RequireComponent(typeof(UiScreen))]
    public sealed class MenuScreen : MonoBehaviour
    {
        private const float IntroSeconds = 0.45f;
        private const float IntroStagger = 0.12f;
        private const float ContentFadeSeconds = 0.25f;

        [SerializeField, Required] private Button _play;
        [SerializeField, Required] private Button _settings;
        [SerializeField, Required] private Button _quit;
        [SerializeField, Required] private UiScreen _quitPrompt;
        [SerializeField, Required] private Button _quitYes;
        [SerializeField, Required] private Button _quitNo;
        [SerializeField, Required] private GameObject _bestRow;
        [SerializeField, Required] private TMP_Text _bestValue;

        [Tooltip("Faded and scaled in one after the other when the menu opens: logo, best run, then the buttons.")]
        [SerializeField] private CanvasGroup[] _intro;

        private UiScreen _screen;
        private ISceneLoader _scenes;
        private InputActionAsset _actions;
        private IBestDistance _best;
        private SettingsScreen _settingsScreen;
        private IHaptics _haptics;
        private IDisposable _backSubscription;
        private bool _loading;

        [Inject]
        public void Construct(ISceneLoader scenes, InputActionAsset actions, IBestDistance best, SettingsScreen settingsScreen, IHaptics haptics)
        {
            _scenes = scenes;
            _actions = actions;
            _best = best;
            _settingsScreen = settingsScreen;
            _haptics = haptics;
        }

        private void Awake() => _screen = GetComponent<UiScreen>();

        private void Start()
        {
            _settingsScreen.Closed += OnSettingsClosed;
            _backSubscription = _actions.FindAction("UI/Cancel", throwIfNotFound: true).Subscribe(_ => OnBack());
            _bestRow.SetActive(_best.Meters > 0);
            _bestValue.text = _best.Meters.ToString("#,0", CultureInfo.InvariantCulture) + " m";
            PlayIntro();
        }

        private void OnEnable()
        {
            _play.onClick.AddListener(OnPlay);
            _settings.onClick.AddListener(OnSettings);
            _quit.onClick.AddListener(OpenQuitPrompt);
            _quitYes.onClick.AddListener(OnQuit);
            _quitNo.onClick.AddListener(CloseQuitPrompt);
        }

        private void OnDisable()
        {
            _play.onClick.RemoveListener(OnPlay);
            _settings.onClick.RemoveListener(OnSettings);
            _quit.onClick.RemoveListener(OpenQuitPrompt);
            _quitYes.onClick.RemoveListener(OnQuit);
            _quitNo.onClick.RemoveListener(CloseQuitPrompt);
        }

        private void OnDestroy()
        {
            _backSubscription?.Dispose();
            if (_settingsScreen != null)
            {
                _settingsScreen.Closed -= OnSettingsClosed;
            }
        }

        private void PlayIntro()
        {
            for (int i = 0; i < _intro.Length; i++)
            {
                var group = _intro[i];
                group.alpha = 0f;
                float delay = i * IntroStagger;
                Tween.Alpha(group, 1f, IntroSeconds, Ease.OutQuad, startDelay: delay, useUnscaledTime: true);
                Tween.Scale(group.transform, 0.92f, 1f, IntroSeconds, Ease.OutBack, startDelay: delay, useUnscaledTime: true);
            }
        }

        private void OnPlay()
        {
            if (_loading)
            {
                return;
            }

            // The load replaces this scene; a second tap would start a second load.
            _loading = true;
            _haptics.Play(HapticKind.Tap);
            _play.interactable = false;
            _scenes.Load<PlayingState>(Scenes.Gameplay).Forget();
        }

        private void OnSettings()
        {
            if (_loading)
            {
                return;
            }

            _haptics.Play(HapticKind.Tap);
            // The menu steps back while the settings are up: transparent, no input, and out of the way (UiScreen switches it off).
            _settingsScreen.Show();
            _screen.Hide();
        }

        private void OnSettingsClosed() => _screen.Show();

        private void OnQuit()
        {
            _haptics.Play(HapticKind.Tap);
            Application.Quit();
        }

        private void OpenQuitPrompt()
        {
            if (!_loading)
            {
                _haptics.Play(HapticKind.Tap);
                _quitPrompt.Show();
                FadeContent(0f);
            }
        }

        private void CloseQuitPrompt()
        {
            _quitPrompt.Hide();
            FadeContent(1f);
        }

        // Only the prompt is left on screen while it is up: the logo, the best run and the buttons fade out (and back).
        private void FadeContent(float alpha)
        {
            foreach (var group in _intro)
            {
                Tween.StopAll(group);
                Tween.Alpha(group, alpha, ContentFadeSeconds, Ease.OutQuad, useUnscaledTime: true);
            }
        }

        private void OnBack()
        {
            if (_loading)
            {
                return;
            }

            switch (MenuBackRule.Next(_settingsScreen.IsOpen, _quitPrompt.IsOpen))
            {
                case MenuBack.CloseSettings:
                    _settingsScreen.Hide();
                    break;
                case MenuBack.CloseQuitPrompt:
                    CloseQuitPrompt();
                    break;
                default:
                    OpenQuitPrompt();
                    break;
            }
        }
    }
}
