using AF.Core;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace WrongLine.Menu
{
    /// <summary>The menu scene's scope: its screen gets the scene loader from here.</summary>
    public sealed class MenuScope : SceneScope
    {
        [SerializeField, Required, Tooltip("The menu screen in this scene.")]
        private MenuScreen _screen;

        protected override void ConfigureScene(IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            builder.RegisterComponent(_screen);
            builder.RegisterEntryPoint<RoadPrewarm>();
            builder.RegisterEntryPoint<SceneryPrewarm>();
        }
    }
}
