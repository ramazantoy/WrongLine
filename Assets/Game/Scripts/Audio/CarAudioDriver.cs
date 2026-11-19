using AF.Audio;
using AF.Core;
using UnityEngine;
using VContainer.Unity;
using WrongLine.Car;
using WrongLine.Driving;
using WrongLine.Traffic;

namespace WrongLine.Audio
{
    /// <summary>
    /// Each frame, feeds the car's speed to <see cref="EngineSoundRule"/> and the result to the loops, and plays the one-shots
    /// that follow from where things are: the whoosh of a traffic car passing and the turn signal's relay. Runs after the car's pose.
    /// </summary>
    internal sealed class CarAudioDriver : IStartable, ITickable
    {
        private readonly CarView _car;
        private readonly ICarMotion _motion;
        private readonly IRoadFrame _road;
        private readonly ITrafficFrame _traffic;
        private readonly IGameClock _clock;
        private readonly ISounds _sounds;
        private CarAudioView _view;
        private SoundHandle _engineLow;
        private SoundHandle _engineHigh;
        private SoundHandle _wind;
        private EngineSoundRule _engine;
        private PassByRule _passBy;
        private SignalClickRule _signal;

        public CarAudioDriver(CarView car, ICarMotion motion, IRoadFrame road, ITrafficFrame traffic, IGameClock clock, ISounds sounds)
        {
            _car = car;
            _motion = motion;
            _road = road;
            _traffic = traffic;
            _clock = clock;
            _sounds = sounds;
        }

        public void Start()
        {
            _view = _car.Audio;
            CarAudioSettings settings = _view.Config.Settings;
            settings.Validate();
            _engine = new EngineSoundRule(settings.Engine);
            _passBy = new PassByRule(settings.PassBy);
            _signal = new SignalClickRule();

            // The loops live as long as this scope; each frame writes their pitch and volume through the handles.
            CarAudioConfig config = _view.Config;
            _engineLow = _sounds.Play(config.EngineLow);
            _engineHigh = _sounds.Play(config.EngineHigh);
            _wind = _sounds.Play(config.Wind);
        }

        public void Tick()
        {
            if (_engine == null)
            {
                return;
            }

            CarAudioConfig config = _view.Config;
            float dt = _clock.DeltaTime;
            _engine.Update(dt, _motion.Speed, _motion.Velocity, _clock.Scale, _motion.Crashed);
            ApplyLoops(_engine.Sound);

            switch (_signal.Observe(_motion.LampOn))
            {
                case SignalClick.Tick:
                    _sounds.Play(config.SignalTick);
                    break;
                case SignalClick.Tock:
                    _sounds.Play(config.SignalTock);
                    break;
            }

            if (!_motion.Crashed)
            {
                PlayPasses(config, dt);
            }
        }

        private void ApplyLoops(in EngineSound sound)
        {
            _engineLow.SetPitch(sound.Pitch);
            _engineHigh.SetPitch(sound.Pitch);
            _engineLow.SetVolume(sound.LowVolume);
            _engineHigh.SetVolume(sound.HighVolume);
            _wind.SetPitch(sound.WindPitch);
            _wind.SetVolume(sound.WindVolume);
        }

        private void PlayPasses(CarAudioConfig config, float dt)
        {
            double time = _traffic.Time;
            double playerDistance = _road.Distance;
            double playerX = _motion.X;
            Transform car = _car.transform;
            _passBy.Begin();
            var active = _traffic.Active;
            for (int i = 0; i < active.Count; i++)
            {
                TrafficCarPlan plan = active[i];
                if (_passBy.Observe(plan.Id, plan.DistanceAt(time) - playerDistance, plan.LaneXAt(time) - playerX, dt, out PassBy pass))
                {
                    // The cue is 3D: put it beside the car, on the side the traffic car went by.
                    _sounds.PlayAt(config.PassBy, car.position + car.right * pass.SideMeters);
                }
            }

            _passBy.End();
        }
    }
}
