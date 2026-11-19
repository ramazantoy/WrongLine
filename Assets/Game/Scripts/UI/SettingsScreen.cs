using System;
using AF.Audio;
using AF.Core;
using AF.Localization;
using AF.Save;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using VContainer;

namespace WrongLine.UI
{
    /// <summary>
    /// The settings screen (a <see cref="UiScreen"/> on the game's UI canvas; it grows in and shrinks out), shared by the menu and the pause menu: three volumes, haptics, language. Values are applied as they
    /// change; closing writes the global and device data (<see cref="ISaveService.SaveGlobal"/>), as the framework's settings expect.
    /// </summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
        [Tooltip("This screen's own fade and grow; the screen is hidden when the scene starts.")]
        [SerializeField, Required] private UiScreen _screen;

        [SerializeField, Required] private Slider _master;
        [SerializeField, Required] private Slider _music;
        [SerializeField, Required] private Slider _sfx;
        [SerializeField, Required] private Toggle _haptics;
        [SerializeField, Required] private Button _previousLanguage;
        [SerializeField, Required] private Button _nextLanguage;
        [SerializeField, Required] private TMP_Text _languageName;
        [SerializeField, Required] private Button _back;

        private IAudioVolumes _volumes;
        private IGameplaySettings _gameplay;
        private ILocaleService _locales;
        private ISaveService _save;

        [Inject]
        public void Construct(IAudioVolumes volumes, IGameplaySettings gameplay, ILocaleService locales, ISaveService save)
        {
            _volumes = volumes;
            _gameplay = gameplay;
            _locales = locales;
            _save = save;
        }

        /// <summary>Raised when the screen is closed, whoever closed it (the Back button, the menu's Back key).</summary>
        public event Action Closed;

        public bool IsOpen => _screen.IsOpen;

        public void Show()
        {
            if (IsOpen)
            {
                return;
            }

            _master.SetValueWithoutNotify(_volumes.Get(AudioChannels.Master));
            _music.SetValueWithoutNotify(_volumes.Get(AudioChannels.Music));
            _sfx.SetValueWithoutNotify(_volumes.Get(AudioChannels.Sfx));
            _haptics.SetIsOnWithoutNotify(_gameplay.Haptics);
            RefreshLanguage();
            _screen.Show();
        }

        public void Hide()
        {
            if (!IsOpen)
            {
                return;
            }

            _screen.Hide();
            Closed?.Invoke();
            // The framework's volumes and language reach the disk now; the game's own settings are in PlayerPrefs already.
            _save.SaveGlobal().Forget();
        }

        private void OnEnable()
        {
            _master.onValueChanged.AddListener(OnMaster);
            _music.onValueChanged.AddListener(OnMusic);
            _sfx.onValueChanged.AddListener(OnSfx);
            _haptics.onValueChanged.AddListener(OnHaptics);
            _previousLanguage.onClick.AddListener(OnPreviousLanguage);
            _nextLanguage.onClick.AddListener(OnNextLanguage);
            _back.onClick.AddListener(Hide);
        }

        private void OnDisable()
        {
            _master.onValueChanged.RemoveListener(OnMaster);
            _music.onValueChanged.RemoveListener(OnMusic);
            _sfx.onValueChanged.RemoveListener(OnSfx);
            _haptics.onValueChanged.RemoveListener(OnHaptics);
            _previousLanguage.onClick.RemoveListener(OnPreviousLanguage);
            _nextLanguage.onClick.RemoveListener(OnNextLanguage);
            _back.onClick.RemoveListener(Hide);
        }

        private void OnMaster(float value) => _volumes.Set(AudioChannels.Master, value);

        private void OnMusic(float value) => _volumes.Set(AudioChannels.Music, value);

        private void OnSfx(float value) => _volumes.Set(AudioChannels.Sfx, value);

        private void OnHaptics(bool on) => _gameplay.SetHaptics(on);

        private void OnPreviousLanguage() => ChangeLanguage(-1).Forget();

        private void OnNextLanguage() => ChangeLanguage(1).Forget();

        private async UniTaskVoid ChangeLanguage(int step)
        {
            var available = _locales.Available;
            int next = LocaleCycle.Step(IndexOf(available, _locales.Current), available.Count, step);
            if (next < 0)
            {
                return;
            }

            try
            {
                await _locales.Set(available[next], destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            RefreshLanguage();
        }

        private void RefreshLanguage()
        {
            var current = _locales.Current;
            _languageName.text = current == null ? string.Empty : LanguageLabel.Format(current.Identifier.CultureInfo.NativeName);
            bool several = _locales.Available.Count > 1;
            _previousLanguage.interactable = several;
            _nextLanguage.interactable = several;
        }

        private static int IndexOf(System.Collections.Generic.IReadOnlyList<Locale> list, Locale locale)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == locale)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
