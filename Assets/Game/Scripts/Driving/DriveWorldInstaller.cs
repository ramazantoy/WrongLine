using MessagePipe;
using UnityEngine;
using UnityEngine.Rendering;
using VContainer;
using VContainer.Unity;
using WrongLine.Car;
using WrongLine.Crash;
using WrongLine.Scenery;
using WrongLine.Traffic;
using WrongLine.World;

namespace WrongLine.Driving
{
    /// <summary>The scene objects and configs the driven world is made of: the car, the camera rig, the sun and the night's bloom.</summary>
    internal readonly struct DriveWorldParts
    {
        public readonly CarConfig CarConfig;
        public readonly CrashConfig CrashConfig;
        public readonly ChaseCameraConfig CameraConfig;
        public readonly CurveWorldConfig CurveConfig;
        public readonly DayNightConfig DayNightConfig;
        public readonly LightsConfig LightsConfig;
        public readonly Volume NightBloom;
        public readonly Light Sun;
        public readonly CarView Car;
        public readonly ChaseCameraView Camera;

        public DriveWorldParts(CarConfig carConfig, CrashConfig crashConfig, ChaseCameraConfig cameraConfig, CurveWorldConfig curveConfig, DayNightConfig dayNightConfig, LightsConfig lightsConfig, Volume nightBloom, Light sun, CarView car, ChaseCameraView camera)
        {
            CarConfig = carConfig;
            CrashConfig = crashConfig;
            CameraConfig = cameraConfig;
            CurveConfig = curveConfig;
            DayNightConfig = dayNightConfig;
            LightsConfig = lightsConfig;
            NightBloom = nightBloom;
            Sun = sun;
            Car = car;
            Camera = camera;
        }
    }

    /// <summary>
    /// The registrations of a scene that drives the road, shared by the scenes that do: the road, the scenery and the traffic
    /// streamed around the car, the camera and the sky that follow it. The scene registers its own steering (the player's input, or
    /// the autopilot's), the road debug readout and whatever else is its own between the two calls: entry points tick in the order
    /// they are registered, so the order of the calls matters.
    /// </summary>
    internal static class DriveWorldInstaller
    {
        /// <summary>Mixed into the run seed for the biomes, so they do not repeat the road generator's numbers.</summary>
        private const ulong BiomeSeedSalt = 0xB10E5EEDC0FFEE11UL;

        /// <summary>Messages, configs, scene objects and everything up to the traffic. Needs <see cref="ISteerInput"/> and <see cref="IRoadDebugHud"/> from the scene.</summary>
        public static void RegisterWorld(IContainerBuilder builder, MessagePipeOptions messagePipe, in DriveWorldParts parts)
        {
            builder.RegisterMessageBroker<RoadOriginShifted>(messagePipe);
            builder.RegisterMessageBroker<CarSideswiped>(messagePipe);
            builder.RegisterMessageBroker<CarCrashed>(messagePipe);

            builder.RegisterInstance(parts.CarConfig);
            builder.RegisterInstance(parts.CrashConfig);
            builder.RegisterInstance(parts.CameraConfig);
            builder.RegisterInstance(parts.CurveConfig);
            builder.RegisterInstance(parts.DayNightConfig);
            builder.RegisterInstance(parts.LightsConfig);
            builder.RegisterComponent(parts.NightBloom);
            builder.RegisterComponent(parts.Sun);
            builder.RegisterComponent(parts.Car);
            builder.RegisterComponent(parts.Camera);

            builder.Register<RunSeed>(Lifetime.Singleton).As<IRunSeed>();
            builder.RegisterEntryPoint<CarController>().As<ICarMotion>().As<ICarImpact>(); // Before the streamer: it reads the speed.
            builder.RegisterEntryPoint<RoadStreamer>().As<IRoadFrame>();
            builder.Register(resolver => new BiomeSchedule(
                resolver.Resolve<IRunSeed>().Value ^ BiomeSeedSalt,
                resolver.Resolve<ISceneryPool>().Set.Biomes.Count,
                resolver.Resolve<SceneryConfig>().Biomes), Lifetime.Singleton);
            builder.RegisterEntryPoint<SceneryStreamer>().As<IRoadsideLamps>(); // After the road streamer: it reads the pieces the road put out.
            builder.RegisterEntryPoint<TrafficStreamer>().As<ITrafficFrame>(); // After the road streamer: it reads where the player is and the road's origin.
        }

        /// <summary>The biome blend and the car's pose on the road.</summary>
        public static void RegisterPlacement(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<BiomeBlendDriver>();
            builder.RegisterEntryPoint<CarPlacer>();
        }

        /// <summary>The camera that chases the car, with the shake of a touch. A scene with another camera registers its own in its place, between the other two calls.</summary>
        public static void RegisterChaseCamera(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<ChaseCameraDriver>(); // Late tick, after the car placer: the rig it writes is what Cinemachine's brain follows.
            builder.RegisterEntryPoint<CameraImpulse>();
        }

        /// <summary>What follows the camera: the curve, the ground, the sky and the lights.</summary>
        public static void RegisterSky(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<CurveWorldDriver>(); // After the camera driver: it reads where the camera ended up.
            builder.RegisterEntryPoint<GroundBackdropDriver>(); // After the camera driver too: it follows the camera.
            builder.RegisterEntryPoint<DayNightDriver>().As<IDayNight>();
            builder.RegisterEntryPoint<LightsDriver>().As<ILightLevels>(); // After the day and night driver: it reads the darkness.
        }
    }
}
