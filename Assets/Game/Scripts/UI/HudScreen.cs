using System;
using AF.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using WrongLine.Haptics;

namespace WrongLine.UI
{
    /// <summary>
    /// The in-game HUD: the run's distance (big, top centre), the speed in km/h, the pause button, and a "new record" label that comes
    /// up once when the run passes the best. <see cref="HudDriver"/> feeds it; it only draws.
    /// </summary>
    [RequireComponent(typeof(UiScreen))]
    public sealed class HudScreen : MonoBehaviour
    {
        [SerializeField, Required] private TMP_Text _distance;
        [SerializeField, Required] private TMP_Text _speed;
        [SerializeField, Required] private Button _pause;
        [SerializeField, Required] private UiScreen _recordLabel;

        private UiScreen _screen;
        private UiConfig _config;
        private IHaptics _haptics;
        private int _shownMeters = -1;
        private int _shownSpeed = -1;
        private float _recordLeft;

        [Inject]
        public void Construct(UiConfig config, IHaptics haptics)
        {
            _config = config;
            _haptics = haptics;
        }

        /// <summary>The pause button was pressed.</summary>
        public event Action PauseClicked;

        private void Awake() => _screen = GetComponent<UiScreen>();

        private void OnEnable() => _pause.onClick.AddListener(OnPause);

        private void OnDisable() => _pause.onClick.RemoveListener(OnPause);

        private void Update()
        {
            if (_recordLeft <= 0f)
            {
                return;
            }

            // Real time: the label is on the screen the same while the game is slowed or paused.
            _recordLeft -= Time.unscaledDeltaTime;
            if (_recordLeft <= 0f)
            {
                _recordLabel.Hide();
            }
        }

        public void Show(int meters, int kmh)
        {
            if (meters != _shownMeters)
            {
                _shownMeters = meters;
                _distance.SetText("{0} m", meters);
            }

            if (kmh != _shownSpeed)
            {
                _shownSpeed = kmh;
                _speed.SetText("{0} km/h", kmh);
            }
        }

        public void ShowRecord()
        {
            _recordLabel.Show();
            _recordLeft = _config.RecordLabelSeconds;
        }

        /// <summary>The run is over: the HUD fades out and its button stops answering.</summary>
        public void Close()
        {
            _recordLeft = 0f;
            _recordLabel.Hide();
            _screen.Hide();
        }

        private void OnPause()
        {
            _haptics.Play(HapticKind.Tap);
            PauseClicked?.Invoke();
        }
    }
}
