using System;
using AF.Core;
using AF.Input;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace WrongLine.Crash
{
    /// <summary>
    /// The game over screen (a placeholder): Retry loads the driving scene again, Menu goes back to the main menu. While it is up
    /// the input is the menu's (only the UI map), so the car no longer steers.
    /// </summary>
    public sealed class GameOverScreen : MonoBehaviour, IGameOverScreen
    {
        [Tooltip("What is shown and hidden: the whole screen except this component's object (which stays active so the scope can find it).")]
        [SerializeField, Required] private GameObject _panel;

        [SerializeField, Required] private Button _retry;
        [SerializeField, Required] private Button _menu;

        private GameOverActions _actions;
        private IInputModeService _input;
        private IDisposable _inputMode;

        [Inject]
        public void Construct(ISceneLoader scenes, IInputModeService input)
        {
            _actions = new GameOverActions(scenes);
            _input = input;
        }

        public void Show()
        {
            if (_panel.activeSelf)
            {
                return;
            }

            _panel.SetActive(true);
            _inputMode = _input.Request(InputModes.Menu);
            _retry.interactable = true;
            _menu.interactable = true;
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

        private void OnRetry()
        {
            Lock();
            _actions.Retry();
        }

        private void OnMenu()
        {
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
