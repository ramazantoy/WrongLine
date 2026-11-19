using System;
using MessagePipe;
using VContainer.Unity;
using WrongLine.Crash;

namespace WrongLine.Haptics
{
    /// <summary>Vibrates for what the car goes through: a medium bump for a sideswipe, a long hard one for the crash.</summary>
    internal sealed class HapticsDriver : IInitializable, IDisposable
    {
        private readonly ISubscriber<CarSideswiped> _sideswipes;
        private readonly ISubscriber<CarCrashed> _crashes;
        private readonly IHaptics _haptics;
        private IDisposable _subscriptions;

        public HapticsDriver(ISubscriber<CarSideswiped> sideswipes, ISubscriber<CarCrashed> crashes, IHaptics haptics)
        {
            _sideswipes = sideswipes;
            _crashes = crashes;
            _haptics = haptics;
        }

        public void Initialize()
        {
            var bag = DisposableBag.CreateBuilder();
            _sideswipes.Subscribe(_ => _haptics.Play(HapticKind.Bump)).AddTo(bag);
            _crashes.Subscribe(_ => _haptics.Play(HapticKind.Crash)).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public void Dispose() => _subscriptions?.Dispose();
    }
}
