using AF.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace WrongLine.Menu
{
    /// <summary>The main menu: Play opens the driving scene.</summary>
    public sealed class MenuScreen : MonoBehaviour
    {
        [SerializeField, Required]
        private Button _play;

        private ISceneLoader _scenes;

        [Inject]
        public void Construct(ISceneLoader scenes) => _scenes = scenes;

        private void OnEnable() => _play.onClick.AddListener(OnPlay);

        private void OnDisable() => _play.onClick.RemoveListener(OnPlay);

        private void OnPlay()
        {
            // The load replaces this scene; a second tap would start a second load.
            _play.interactable = false;
            _scenes.Load<PlayingState>(Scenes.Gameplay).Forget();
        }
    }
}
