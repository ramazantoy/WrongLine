using AF.Core;
using VContainer.Unity;
using WrongLine.Car;

namespace WrongLine.Driving
{
    /// <summary>Moves the camera behind the car each frame, after everything else has moved.</summary>
    internal sealed class ChaseCameraDriver : IPostLateTickable
    {
        private readonly ChaseCameraView _camera;
        private readonly IRoadFrame _road;
        private readonly ICarMotion _motion;
        private readonly ChaseCameraConfig _config;
        private readonly IGameClock _clock;
        private readonly ChaseCameraRule _rule;

        public ChaseCameraDriver(ChaseCameraView camera, IRoadFrame road, ICarMotion motion, ChaseCameraConfig config, IGameClock clock)
        {
            _camera = camera;
            _road = road;
            _motion = motion;
            _config = config;
            _clock = clock;
            // The rule reads the config's settings object every frame, so tuning it in the Inspector shows immediately.
            _rule = new ChaseCameraRule(config.Settings);
        }

        public void PostLateTick()
        {
            // Built from the road's doubles every frame: an origin shift needs no special care here.
            ChaseTarget target = ChaseTarget.At(_road.Path, _road.Origin, _road.Distance, _motion.X, _motion.Speed, _config.Settings);
            _rule.Update(target, _clock.DeltaTime);
            _camera.Apply(_rule.Position, _rule.Rotation, _rule.Fov);
        }
    }
}
