using AF.Core;
using Cysharp.Threading.Tasks;

namespace WrongLine.Crash
{
    /// <summary>
    /// What the game over screen's buttons do: start the driving scene again, or go back to the menu. Only one of them, once: the
    /// load replaces the scene, and a second tap before it has finished would start a second load. Pure apart from the loader.
    /// </summary>
    public sealed class GameOverActions
    {
        private readonly ISceneLoader _scenes;

        public GameOverActions(ISceneLoader scenes)
        {
            _scenes = scenes;
        }

        /// <summary>True once either button has been used.</summary>
        public bool Used { get; private set; }

        public void Retry()
        {
            if (Used)
            {
                return;
            }

            Used = true;
            _scenes.Load(Scenes.Gameplay).Forget();
        }

        public void Menu()
        {
            if (Used)
            {
                return;
            }

            Used = true;
            _scenes.Load<MenuState>(Scenes.Menu).Forget();
        }
    }
}
