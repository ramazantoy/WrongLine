using System;
using AF.Input;
using MessagePipe;
using UnityEngine.InputSystem;
using VContainer.Unity;
using WrongLine.Car;

namespace WrongLine.UI
{
    /// <summary>Opens and closes the pause menu: the HUD's pause button and Back (the Android back key, Escape), by <see cref="PauseBackRule"/>.</summary>
    internal sealed class PauseDriver : IInitializable, IDisposable
    {
        private readonly InputActionAsset _actions;
        private readonly PauseScreen _pause;
        private readonly SettingsScreen _settings;
        private readonly HudScreen _hud;
        private readonly ICarMotion _motion;
        private IDisposable _back;

        public PauseDriver(InputActionAsset actions, PauseScreen pause, SettingsScreen settings, HudScreen hud, ICarMotion motion)
        {
            _actions = actions;
            _pause = pause;
            _settings = settings;
            _hud = hud;
            _motion = motion;
        }

        public void Initialize()
        {
            _hud.PauseClicked += OnPauseClicked;
            // UI/Cancel only: Gameplay/Pause is bound to Escape too, and listening to both would act twice.
            _back = _actions.FindAction("UI/Cancel", throwIfNotFound: true).Subscribe(_ => OnBack());
        }

        public void Dispose()
        {
            _back?.Dispose();
            if (_hud != null)
            {
                _hud.PauseClicked -= OnPauseClicked;
            }
        }

        private void OnPauseClicked()
        {
            if (!_motion.Crashed)
            {
                _pause.Open();
            }
        }

        private void OnBack()
        {
            switch (PauseBackRule.Next(_motion.Crashed, _pause.IsOpen, _settings.IsOpen))
            {
                case PauseBack.OpenPause:
                    _pause.Open();
                    break;
                case PauseBack.CloseSettings:
                    _settings.Hide();
                    break;
                case PauseBack.Resume:
                    _pause.Resume();
                    break;
            }
        }
    }
}
