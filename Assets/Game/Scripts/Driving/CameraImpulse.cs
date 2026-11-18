using System;
using MessagePipe;
using VContainer.Unity;
using WrongLine.Crash;

namespace WrongLine.Driving
{
    /// <summary>Shakes the camera when the car is sideswiped and when it crashes, by the strengths and durations in <see cref="CrashConfig"/>.</summary>
    internal sealed class CameraImpulse : IStartable, IDisposable
    {
        private readonly ChaseCameraView _camera;
        private readonly CrashConfig _config;
        private readonly ISubscriber<CarSideswiped> _sideswipes;
        private readonly ISubscriber<CarCrashed> _crashes;
        private IDisposable _subscriptions;

        public CameraImpulse(ChaseCameraView camera, CrashConfig config, ISubscriber<CarSideswiped> sideswipes, ISubscriber<CarCrashed> crashes)
        {
            _camera = camera;
            _config = config;
            _sideswipes = sideswipes;
            _crashes = crashes;
        }

        public void Start()
        {
            var bag = DisposableBag.CreateBuilder();
            _sideswipes.Subscribe(OnSideswiped).AddTo(bag);
            _crashes.Subscribe(OnCrashed).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public void Dispose()
        {
            _subscriptions?.Dispose();
        }

        private void OnSideswiped(CarSideswiped swipe)
        {
            CrashSettings.CameraGroup group = _config.Settings.Camera;
            _camera.Shake(group.SideswipeShake, group.SideswipeShakeSeconds);
        }

        private void OnCrashed(CarCrashed crash)
        {
            CrashSettings.CameraGroup group = _config.Settings.Camera;
            _camera.Shake(group.CrashShake, group.CrashShakeSeconds);
        }
    }
}
