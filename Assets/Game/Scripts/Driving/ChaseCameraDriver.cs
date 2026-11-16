using System;
using AF.Core;
using MessagePipe;
using VContainer.Unity;

namespace WrongLine.Driving
{
    /// <summary>Moves the camera behind the follower each frame, after everything else has moved.</summary>
    internal sealed class ChaseCameraDriver : IPostLateTickable, IDisposable
    {
        private readonly ChaseCameraView _camera;
        private readonly DummyFollowerView _follower;
        private readonly IGameClock _clock;
        private readonly ChaseCameraRule _rule;
        private readonly IDisposable _subscription;

        public ChaseCameraDriver(ChaseCameraView camera, DummyFollowerView follower, ChaseCameraConfig config, IGameClock clock, ISubscriber<RoadOriginShifted> shifted)
        {
            _camera = camera;
            _follower = follower;
            _clock = clock;
            // The rule reads the config's settings object every frame, so tuning it in the Inspector shows immediately.
            _rule = new ChaseCameraRule(config.Settings);
            _subscription = shifted.Subscribe(message => _rule.Shift(message.Shift));
        }

        public void PostLateTick()
        {
            _rule.Update(_follower.Position, _follower.Rotation, _clock.DeltaTime);
            _camera.Apply(_rule.Position, _rule.Rotation);
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }
    }
}
