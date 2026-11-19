using System;
using AF.Audio;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;
using WrongLine.Car;
using WrongLine.Crash;

namespace WrongLine.Audio
{
    /// <summary>The sound of what the car goes through: a scrape for a sideswipe, metal and glass for the crash (the music fades out with it).</summary>
    internal sealed class CrashAudioDriver : IInitializable, IDisposable
    {
        private const float MusicFadeSeconds = 1.2f;

        private readonly CarView _car;
        private readonly ISounds _sounds;
        private readonly IMusic _music;
        private readonly ISubscriber<CarSideswiped> _sideswipes;
        private readonly ISubscriber<CarCrashed> _crashes;
        private IDisposable _subscriptions;

        public CrashAudioDriver(CarView car, ISounds sounds, IMusic music, ISubscriber<CarSideswiped> sideswipes, ISubscriber<CarCrashed> crashes)
        {
            _car = car;
            _sounds = sounds;
            _music = music;
            _sideswipes = sideswipes;
            _crashes = crashes;
        }

        public void Initialize()
        {
            var bag = DisposableBag.CreateBuilder();
            _sideswipes.Subscribe(OnSideswipe).AddTo(bag);
            _crashes.Subscribe(OnCrash).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public void Dispose() => _subscriptions?.Dispose();

        private void OnSideswipe(CarSideswiped swipe)
        {
            var transform = _car.transform;
            _sounds.PlayAt(_car.Audio.Config.Scrape, transform.position + transform.right * swipe.Side);
        }

        private void OnCrash(CarCrashed crash)
        {
            CarAudioConfig config = _car.Audio.Config;
            Vector3 position = _car.transform.position;
            _sounds.PlayAt(config.CrashMetal, position);
            _sounds.PlayAt(config.CrashGlass, position);
            _music.Stop(MusicFadeSeconds);
        }
    }
}
