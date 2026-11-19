using AF.Core;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using WrongLine.Attract;
using WrongLine.Audio;
using WrongLine.Car;
using WrongLine.Driving;
using WrongLine.UI;

namespace WrongLine.Menu
{
    /// <summary>
    /// The menu scene's scope: its screens get the scene loader, the settings and the records from here. Behind them the road runs
    /// with the player's car on it, an autopilot at the wheel (it never touches a car, nothing crashes), under the game's day and night.
    /// </summary>
    public sealed class MenuScope : SceneScope
    {
        [SerializeField, Required, Tooltip("The menu screen in this scene.")]
        private MenuScreen _screen;

        [SerializeField, Required, Tooltip("The settings screen, opened from the menu.")]
        private SettingsScreen _settings;

        [SerializeField, Required, Tooltip("The car, camera rig, sun and configs of the drive behind the menu.")]
        private MenuDriveRig _drive;

        protected override void ConfigureScene(IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            builder.RegisterComponent(_screen);
            builder.RegisterComponent(_settings);
            builder.RegisterEntryPoint<MenuMusic>();
            builder.RegisterEntryPoint<RoadPrewarm>();
            builder.RegisterEntryPoint<SceneryPrewarm>();
            builder.RegisterEntryPoint<TrafficPrewarm>();

            // The pools are the root's: the streamers take their pieces from them and give them back when this scope ends.
            builder.RegisterComponent(_drive);
            builder.RegisterInstance(_drive.Autopilot);
            builder.RegisterInstance(_drive.FirstPerson);
            builder.RegisterInstance<IRoadDebugHud>(new NullRoadDebugHud());
            builder.Register<AutopilotSteerInput>(Lifetime.Singleton).AsSelf().As<ISteerInput>();
            DriveWorldInstaller.RegisterWorld(builder, messagePipe, _drive.Parts);
            builder.RegisterEntryPoint<AutopilotDriver>(); // After the traffic: it reads the lane the traffic keeps clear.
            DriveWorldInstaller.RegisterPlacement(builder);
            builder.RegisterEntryPoint<FirstPersonCameraDriver>(); // The camera rides in the car instead of chasing it; nothing touches it, so there is no shake.
            DriveWorldInstaller.RegisterSky(builder);
        }
    }
}
