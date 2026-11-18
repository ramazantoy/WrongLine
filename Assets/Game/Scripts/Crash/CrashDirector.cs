using System;
using AF.Core;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;
using WrongLine.Driving;

namespace WrongLine.Crash
{
    /// <summary>
    /// Runs what follows a crash: lays the ground for the wreck, slows the game down (<see cref="IGameClock.Scale"/>) and when
    /// <see cref="CrashTimelineRule"/> says so brings up the game over screen. Puts the game's speed back when the scene ends,
    /// if it is still the one it set.
    /// </summary>
    internal sealed class CrashDirector : ITickable, IDisposable
    {
        private readonly IGameClock _clock;
        private readonly CrashConfig _config;
        private readonly IRoadFrame _road;
        private readonly IGameOverScreen _screen;
        private readonly IDisposable _subscription;
        private CrashTimelineRule _timeline;
        private GameObject _ground;
        private float _written = 1f;
        private bool _shown;

        public CrashDirector(ISubscriber<CarCrashed> crashes, IGameClock clock, CrashConfig config, IRoadFrame road, IGameOverScreen screen)
        {
            _clock = clock;
            _config = config;
            _road = road;
            _screen = screen;
            _subscription = crashes.Subscribe(OnCrashed);
        }

        public void Tick()
        {
            if (_timeline == null || _shown)
            {
                return;
            }

            // The clock's frame time is already scaled; the timeline counts real seconds. Paused: 0 either way.
            float scale = _clock.Scale;
            _timeline.Update(scale > 0f ? _clock.DeltaTime / scale : 0f);
            _clock.Scale = _timeline.Scale;
            _written = _timeline.Scale;

            if (_timeline.GameOverDue)
            {
                _shown = true;
                _screen.Show();
            }
        }

        public void Dispose()
        {
            _subscription.Dispose();
            if (_timeline != null && Mathf.Approximately(_clock.Scale, _written))
            {
                _clock.Scale = 1f; // Only if nobody else changed it since.
            }

            if (_ground != null)
            {
                UnityEngine.Object.Destroy(_ground);
            }
        }

        private void OnCrashed(CarCrashed crash)
        {
            _timeline = new CrashTimelineRule(_config.Settings.SlowMotion);
            _ground = CrashGround.Build(_road.Path, _road.Origin, crash.Distance, _config.Settings.Wreck);
        }
    }
}
