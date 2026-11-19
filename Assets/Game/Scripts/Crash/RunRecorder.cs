using System;
using MessagePipe;
using VContainer.Unity;
using WrongLine.Car;
using WrongLine.UI;

namespace WrongLine.Crash
{
    /// <summary>
    /// Writes the run into the records when the car crashes: the distance it covered goes to <see cref="IBestDistance"/>, and the
    /// result is kept for the game over screen. Registered before the crash director, so the result is there when the screen comes up.
    /// </summary>
    internal sealed class RunRecorder : IInitializable, IDisposable, IRunResult
    {
        private readonly ISubscriber<CarCrashed> _crashes;
        private readonly ICarMotion _motion;
        private readonly IBestDistance _best;
        private IDisposable _subscription;

        public RunRecorder(ISubscriber<CarCrashed> crashes, ICarMotion motion, IBestDistance best)
        {
            _crashes = crashes;
            _motion = motion;
            _best = best;
        }

        public int Meters { get; private set; }

        public int Best => _best.Meters;

        public bool IsRecord { get; private set; }

        public void Initialize() => _subscription = _crashes.Subscribe(OnCrashed);

        public void Dispose() => _subscription?.Dispose();

        internal void OnCrashed(CarCrashed crash)
        {
            // The car's own distance counts from the start of the run; the crash's is a place on the road, which may not begin at 0.
            double distance = _motion.Distance;
            IsRecord = _best.Submit(distance);
            Meters = double.IsNaN(distance) || distance <= 0 ? 0 : (int)Math.Min(Math.Floor(distance), int.MaxValue);
        }
    }
}
