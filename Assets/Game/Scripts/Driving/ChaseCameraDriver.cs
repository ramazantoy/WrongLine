using AF.Core;
using UnityEngine;
using VContainer.Unity;
using WrongLine.Car;
using WrongLine.Crash;
using WrongLine.Road;

namespace WrongLine.Driving
{
    /// <summary>
    /// Moves the camera behind the car each frame, after everything else has moved. After a crash it keeps to the road but follows
    /// the wreck, forwards or back, so the wreck stays in the picture at the same angle (<see cref="CrashCameraRule"/>).
    /// </summary>
    internal sealed class ChaseCameraDriver : IPostLateTickable
    {
        private readonly ChaseCameraView _camera;
        private readonly IRoadFrame _road;
        private readonly ICarMotion _motion;
        private readonly ChaseCameraConfig _config;
        private readonly IGameClock _clock;
        private readonly CarView _car;
        private readonly CrashConfig _crash;
        private readonly ChaseCameraRule _rule;
        private CrashCameraRule _crashRule;
        private double _wreckDistance;

        public ChaseCameraDriver(ChaseCameraView camera, IRoadFrame road, ICarMotion motion, ChaseCameraConfig config, IGameClock clock, CarView car, CrashConfig crash)
        {
            _camera = camera;
            _road = road;
            _motion = motion;
            _config = config;
            _clock = clock;
            _car = car;
            _crash = crash;
            // The rule reads the config's settings object every frame, so tuning it in the Inspector shows immediately.
            _rule = new ChaseCameraRule(config.Settings);
        }

        public void PostLateTick()
        {
            // Built from the road's doubles every frame: an origin shift needs no special care here.
            ChaseTarget target = _car.IsWrecked
                ? WreckTarget()
                : ChaseTarget.At(_road.Path, _road.Origin, _road.Distance, _motion.X, _motion.Speed, _config.Settings);
            _rule.Update(target, _clock.DeltaTime);
            _camera.Apply(_rule.Position, _rule.Rotation, _rule.Fov);
        }

        private ChaseTarget WreckTarget()
        {
            if (_crashRule == null)
            {
                _wreckDistance = _road.Distance;
                _crashRule = new CrashCameraRule(_crash.Settings.Camera, _wreckDistance);
            }

            IRoadPath path = _road.Path;
            WorldPoint origin = _road.Origin;
            Vector3 wreck = _car.WreckBody.position;
            _wreckDistance = RoadProjection.DistanceOf(path, origin, wreck, _wreckDistance);
            _crashRule.Update(_wreckDistance, _clock.DeltaTime);

            ChaseCameraSettings settings = _config.Settings;
            RoadSample rear = RoadSampler.At(path, _crashRule.Distance - settings.Distance);
            RoadSample ahead = RoadSampler.At(path, _crashRule.Distance + settings.LookAhead);
            return new ChaseTarget(rear.Position.RelativeTo(origin), rear.Rotation, ahead.Position.RelativeTo(origin), ahead.Rotation, wreck, _motion.X, 0f);
        }
    }
}
