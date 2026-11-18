using System;
using AF.Core;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;
using WrongLine.Car;
using WrongLine.Crash;
using WrongLine.Road;

namespace WrongLine.Driving
{
    /// <summary>
    /// Each frame, after the car has been placed, works out where the camera should be and look (<see cref="CameraDollyRule"/> for
    /// where along the road, <see cref="ChaseCameraRule"/> for the pose) and puts the rig there; Cinemachine's brain then moves the
    /// camera to it, and the world drivers that read the camera run after that. The subject is the car; after a crash the wreck,
    /// forwards or back along the road, so it stays in the picture at the same angle.
    /// </summary>
    internal sealed class ChaseCameraDriver : ILateTickable, IDisposable
    {
        private readonly ChaseCameraView _camera;
        private readonly IRoadFrame _road;
        private readonly ICarMotion _motion;
        private readonly ChaseCameraConfig _config;
        private readonly IGameClock _clock;
        private readonly CarView _car;
        private readonly CrashConfig _crash;
        private readonly ChaseCameraRule _rule;
        private readonly CameraDollyRule _dolly;
        private readonly IDisposable _subscription;
        private double _wreckDistance;
        private bool _wreckKnown;

        public ChaseCameraDriver(ChaseCameraView camera, IRoadFrame road, ICarMotion motion, ChaseCameraConfig config, IGameClock clock, CarView car, CrashConfig crash, ISubscriber<CarCrashed> crashes)
        {
            _camera = camera;
            _road = road;
            _motion = motion;
            _config = config;
            _clock = clock;
            _car = car;
            _crash = crash;
            // The rules read the config's settings object every frame, so tuning it in the Inspector shows immediately.
            _rule = new ChaseCameraRule(config.Settings);
            _dolly = new CameraDollyRule(config.Settings);
            _subscription = crashes.Subscribe(OnCrashed);
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }

        public void LateTick()
        {
            // Built from the road's doubles every frame: an origin shift needs no special care here.
            ChaseCameraSettings settings = _config.Settings;
            IRoadPath path = _road.Path;
            WorldPoint origin = _road.Origin;

            bool wrecked = _car.IsWrecked;
            Vector3 subjectPosition;
            double subject;
            if (wrecked)
            {
                subjectPosition = _car.WreckBody.position;
                _wreckDistance = RoadProjection.DistanceOf(path, origin, subjectPosition, _wreckKnown ? _wreckDistance : _road.Distance);
                _wreckKnown = true;
                subject = _wreckDistance;
            }
            else
            {
                subject = _road.Distance;
                subjectPosition = RoadSampler.At(path, subject, _motion.X).Position.RelativeTo(origin);
            }

            // In a bend the camera backs off a little: how sharp it is, from the road's direction where the camera would nominally stand and where it looks.
            RoadSample ahead = RoadSampler.At(path, subject + settings.LookAhead);
            float bend = CameraDollyRule.Bend(RoadSampler.At(path, subject - settings.Distance).Rotation, ahead.Rotation, settings);
            _dolly.Update(subject, _clock.DeltaTime, bend);

            RoadSample rear = RoadSampler.At(path, _dolly.Distance);
            var target = new ChaseTarget(rear.Position.RelativeTo(origin), rear.Rotation, ahead.Position.RelativeTo(origin), ahead.Rotation, subjectPosition, _motion.X, wrecked ? 0f : _motion.Speed);
            _rule.Update(target, _clock.DeltaTime);
            _camera.Apply(_rule.Position, _rule.Rotation, _rule.Fov);
        }

        private void OnCrashed(CarCrashed crash)
        {
            _camera.ShowCrashView(_crash.Settings.Camera.CrashFov, _crash.Settings.Camera.BlendSeconds);
        }
    }
}
