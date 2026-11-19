using AF.Core;
using MessagePipe;
using UnityEngine;
using UnityEngine.Rendering;
using VContainer;
using VContainer.Unity;
using WrongLine.Car;
using WrongLine.Crash;
using WrongLine.Haptics;
using WrongLine.UI;
using WrongLine.World;

namespace WrongLine.Driving
{
    /// <summary>
    /// The driving scene's scope: the endless road streamed around the player's car (<see cref="DriveWorldInstaller"/>, shared with
    /// the menu's background drive), a chase camera behind it, the crash, and the screens on the UI canvas: HUD, pause, settings,
    /// game over and a debug readout. The road pieces themselves come from the root's pool.
    /// </summary>
    public sealed class DrivingScope : SceneScope
    {
        [SerializeField, Required] private CarConfig _carConfig;
        [SerializeField, Required] private ChaseCameraConfig _cameraConfig;
        [SerializeField, Required] private CurveWorldConfig _curveConfig;
        [SerializeField, Required] private DayNightConfig _dayNightConfig;
        [SerializeField, Required] private LightsConfig _lightsConfig;
        [SerializeField, Required] private Volume _nightBloom;
        [SerializeField, Required] private Light _sun;
        [SerializeField, Required] private CarView _car;
        [SerializeField, Required] private ChaseCameraView _camera;
        [SerializeField, Required] private RoadDebugHud _hud;
        [SerializeField, Required] private CrashConfig _crashConfig;
        [SerializeField, Required] private UiConfig _uiConfig;
        [SerializeField, Required] private HudScreen _hudScreen;
        [SerializeField, Required] private PauseScreen _pauseScreen;
        [SerializeField, Required] private SettingsScreen _settings;
        [SerializeField, Required] private GameOverScreen _gameOver;

        protected override void ConfigureScene(IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            builder.RegisterInstance(_uiConfig);
            builder.RegisterComponent(_hud).As<IRoadDebugHud>();
            builder.RegisterComponent(_hudScreen);
            builder.RegisterComponent(_pauseScreen);
            builder.RegisterComponent(_settings);
            builder.RegisterComponent(_gameOver).As<IGameOverScreen>();

            builder.Register<SteerInput>(Lifetime.Singleton).As<ISteerInput>();
            DriveWorldInstaller.RegisterWorld(builder, messagePipe, new DriveWorldParts(_carConfig, _crashConfig, _cameraConfig, _curveConfig, _dayNightConfig, _lightsConfig, _nightBloom, _sun, _car, _camera));
            builder.RegisterEntryPoint<CrashDetector>(); // After the road and the traffic: it reads where everything is this frame.
            builder.RegisterEntryPoint<RunRecorder>().As<IRunResult>(); // Before the director: the game over screen reads the result it keeps.
            builder.RegisterEntryPoint<CrashDirector>();
            DriveWorldInstaller.RegisterPlacement(builder);
            DriveWorldInstaller.RegisterChaseCamera(builder);
            DriveWorldInstaller.RegisterSky(builder);

            builder.RegisterEntryPoint<HudDriver>();
            builder.RegisterEntryPoint<PauseDriver>();
            builder.RegisterEntryPoint<HapticsDriver>();
        }
    }
}
