using System;
using AF.Audio;
using AF.Core;
using AF.Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using WrongLine.Audio;
using WrongLine.UI;

namespace WrongLine.Crash
{
    /// <summary>
    /// The game over screen: the run's distance counts up from 0, the best run is shown (and, if this run is the new best, a
    /// "new record" label), and Retry loads the driving scene again while Menu goes back to the main menu. While it is up the input
    /// is the menu's (only the UI map), so the car no longer steers.
    /// </summary>
    [RequireComponent(typeof(UiScreen))]
    public sealed class GameOverScreen : MonoBehaviour, IGameOverScreen
    {
        [SerializeField, Required] private TMP_Text _distance;
        [SerializeField, Required] private TMP_Text _best;
        [SerializeField, Required] private GameObject _recordLabel;
        [SerializeField, Required] private Button _retry;
        [SerializeField, Required] private Button _menu;

        private UiScreen _screen;
        private GameOverActions _actions;
        private IInputModeService _input;
        private IRunResult _result;
        private UiConfig _config;
        private IUiFeedback _feedback;
        private ISounds _sounds;
        private GameAudioConfig _audio;
        private IDisposable _inputMode;
        private float _elapsed;
        private int _shown = -1;
        private bool _counting;

        [Inject]
        public void Construct(ISceneLoader scenes, IInputModeService input, IRunResult result, UiConfig config, IUiFeedback feedback, ISounds sounds, GameAudioConfig audio)
        {
            _actions = new GameOverActions(scenes);
            _input = input;
            _result = result;
            _config = config;
            _feedback = feedback;
            _sounds = sounds;
            _audio = audio;
        }

        // Not set in Awake: the screen starts inactive and is shown before Unity has run it.
        private UiScreen Screen => _screen != null ? _screen : (_screen = GetComponent<UiScreen>());

        public void Show()
        {
            if (Screen.IsOpen)
            {
                return;
            }

            Screen.Show();
            _inputMode = _input.Request(InputModes.Menu);
            _retry.interactable = true;
            _menu.interactable = true;
            _recordLabel.SetActive(_result.IsRecord);
            _sounds.Play(_result.IsRecord ? _audio.RecordJingle : _audio.GameOverJingle);
            _best.SetText("{0} m", _result.Best);
            _elapsed = 0f;
            _shown = -1;
            _counting = true;
            Count();
        }

        private void OnEnable()
        {
            _retry.onClick.AddListener(OnRetry);
            _menu.onClick.AddListener(OnMenu);
        }

        private void OnDisable()
        {
            _retry.onClick.RemoveListener(OnRetry);
            _menu.onClick.RemoveListener(OnMenu);
        }

        private void OnDestroy()
        {
            _inputMode?.Dispose();
        }

        private void Update()
        {
            if (!_counting)
            {
                return;
            }

            // Real time: the game is still in slow motion or paused when the screen comes up.
            _elapsed += Time.unscaledDeltaTime;
            Count();
        }

        private void Count()
        {
            int value = CountUpRule.Value(_result.Meters, _elapsed - _config.CountUpDelay, _config.CountUpSeconds);
            if (value != _shown)
            {
                _shown = value;
                _distance.SetText("{0} m", value);
            }

            _counting = value < _result.Meters;
        }

        private void OnRetry()
        {
            _feedback.Play(UiFeedbackKind.Tap);
            Lock();
            _actions.Retry();
        }

        private void OnMenu()
        {
            _feedback.Play(UiFeedbackKind.Back);
            Lock();
            _actions.Menu();
        }

        private void Lock()
        {
            _retry.interactable = false;
            _menu.interactable = false;
        }
    }
}
