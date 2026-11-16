using AF.Core;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using WrongLine.World;

namespace WrongLine.Driving
{
    /// <summary>
    /// The driving scene's scope: the endless road streamed around a dummy follower, a chase camera behind it, and a debug
    /// readout. The road pieces themselves come from the root's pool.
    /// </summary>
    public sealed class DrivingScope : SceneScope
    {
        [SerializeField, Required] private FollowerConfig _followerConfig;
        [SerializeField, Required] private ChaseCameraConfig _cameraConfig;
        [SerializeField, Required] private CurveWorldConfig _curveConfig;
        [SerializeField, Required] private DayNightConfig _dayNightConfig;
        [SerializeField, Required] private Light _sun;
        [SerializeField, Required] private DummyFollowerView _follower;
        [SerializeField, Required] private ChaseCameraView _camera;
        [SerializeField, Required] private RoadDebugHud _hud;

        protected override void ConfigureScene(IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            builder.RegisterMessageBroker<RoadOriginShifted>(messagePipe);

            builder.RegisterInstance(_followerConfig);
            builder.RegisterInstance(_cameraConfig);
            builder.RegisterInstance(_curveConfig);
            builder.RegisterInstance(_dayNightConfig);
            builder.RegisterComponent(_sun);
            builder.RegisterComponent(_follower);
            builder.RegisterComponent(_camera);
            builder.RegisterComponent(_hud).As<IRoadDebugHud>();

            builder.RegisterEntryPoint<RoadStreamer>();
            builder.RegisterEntryPoint<ChaseCameraDriver>();
            builder.RegisterEntryPoint<CurveWorldDriver>(); // After the camera driver: it reads where the camera ended up.
            builder.RegisterEntryPoint<DayNightDriver>();
        }
    }
}
