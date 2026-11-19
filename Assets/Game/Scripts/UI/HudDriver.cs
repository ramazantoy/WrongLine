using System;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;
using WrongLine.Car;
using WrongLine.Crash;

namespace WrongLine.UI
{
    /// <summary>Each frame feeds the HUD the run's distance and speed (<see cref="HudRule"/> says what changed), and closes it at the crash.</summary>
    internal sealed class HudDriver : IInitializable, ITickable, IDisposable
    {
        private const float KmhPerMetreSecond = 3.6f;

        private readonly HudScreen _hud;
        private readonly ICarMotion _motion;
        private readonly IBestDistance _best;
        private readonly ISubscriber<CarCrashed> _crashes;
        private HudRule _rule;
        private IDisposable _subscription;
        private bool _closed;

        public HudDriver(HudScreen hud, ICarMotion motion, IBestDistance best, ISubscriber<CarCrashed> crashes)
        {
            _hud = hud;
            _motion = motion;
            _best = best;
            _crashes = crashes;
        }

        public void Initialize()
        {
            // The best that stood before this run: the recorder only writes at the crash.
            _rule = new HudRule(_best.Meters);
            _subscription = _crashes.Subscribe(_ =>
            {
                _closed = true;
                _hud.Close();
            });
        }

        public void Tick()
        {
            if (_closed)
            {
                return;
            }

            var frame = _rule.Update(_motion.Distance);
            _hud.Show(frame.Meters, Mathf.RoundToInt(_motion.Speed * KmhPerMetreSecond));
            if (frame.NewRecord)
            {
                _hud.ShowRecord();
            }
        }

        public void Dispose() => _subscription?.Dispose();
    }
}
