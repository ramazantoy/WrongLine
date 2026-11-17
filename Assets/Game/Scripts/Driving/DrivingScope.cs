using AF.Core;
using MessagePipe;
using UnityEngine;
using UnityEngine.Rendering;
using VContainer;
using VContainer.Unity;
using WrongLine.Car;
using WrongLine.Scenery;
using WrongLine.Traffic;
using WrongLine.World;

namespace WrongLine.Driving
{
    /// <summary>
    /// The driving scene's scope: the endless road streamed around the player's car, a chase camera behind it, and a debug
    /// readout. The road pieces themselves come from the root's pool.
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

        /// <summary>Mixed into the run seed for the biomes, so they do not repeat the road generator's numbers.</summary>
        private const ulong BiomeSeedSalt = 0xB10E5EEDC0FFEE11UL;

        protected override void ConfigureScene(IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            builder.RegisterMessageBroker<RoadOriginShifted>(messagePipe);

            builder.RegisterInstance(_carConfig);
            builder.RegisterInstance(_cameraConfig);
            builder.RegisterInstance(_curveConfig);
            builder.RegisterInstance(_dayNightConfig);
            builder.RegisterInstance(_lightsConfig);
            builder.RegisterComponent(_nightBloom);
            builder.RegisterComponent(_sun);
            builder.RegisterComponent(_car);
            builder.RegisterComponent(_camera);
            builder.RegisterComponent(_hud).As<IRoadDebugHud>();

            builder.Register<RunSeed>(Lifetime.Singleton).As<IRunSeed>();
            builder.Register<SteerInput>(Lifetime.Singleton).As<ISteerInput>();
            builder.RegisterEntryPoint<CarController>().As<ICarMotion>(); // Before the streamer: it reads the speed.
            builder.RegisterEntryPoint<RoadStreamer>().As<IRoadFrame>();
            builder.Register(resolver => new BiomeSchedule(
                resolver.Resolve<IRunSeed>().Value ^ BiomeSeedSalt,
                resolver.Resolve<ISceneryPool>().Set.Biomes.Count,
                resolver.Resolve<SceneryConfig>().Biomes), Lifetime.Singleton);
            builder.RegisterEntryPoint<SceneryStreamer>().As<IRoadsideLamps>(); // After the road streamer: it reads the pieces the road put out.
            builder.RegisterEntryPoint<TrafficStreamer>().As<ITrafficFrame>(); // After the road streamer: it reads where the player is and the road's origin.
            builder.RegisterEntryPoint<BiomeBlendDriver>();
            builder.RegisterEntryPoint<CarPlacer>();
            builder.RegisterEntryPoint<ChaseCameraDriver>();
            builder.RegisterEntryPoint<CurveWorldDriver>(); // After the camera driver: it reads where the camera ended up.
            builder.RegisterEntryPoint<GroundBackdropDriver>(); // After the camera driver too: it follows the camera.
            builder.RegisterEntryPoint<DayNightDriver>().As<IDayNight>();
            builder.RegisterEntryPoint<LightsDriver>().As<ILightLevels>(); // After the day and night driver: it reads the darkness.
        }
    }
}
