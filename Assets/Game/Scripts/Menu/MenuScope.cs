using AF.Core;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using WrongLine.UI;

namespace WrongLine.Menu
{
    /// <summary>The menu scene's scope: its screens get the scene loader, the settings and the records from here.</summary>
    public sealed class MenuScope : SceneScope
    {
        [SerializeField, Required, Tooltip("The menu screen in this scene.")]
        private MenuScreen _screen;

        [SerializeField, Required, Tooltip("The settings screen, opened from the menu.")]
        private SettingsScreen _settings;

        protected override void ConfigureScene(IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            builder.RegisterComponent(_screen);
            builder.RegisterComponent(_settings);
            builder.RegisterEntryPoint<RoadPrewarm>();
            builder.RegisterEntryPoint<SceneryPrewarm>();
            builder.RegisterEntryPoint<TrafficPrewarm>();
        }
    }
}
