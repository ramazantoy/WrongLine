using System;
using AF.Core;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;
using WrongLine.Crash;
using WrongLine.Driving;
using WrongLine.Road;
using WrongLine.World;

namespace WrongLine.Car
{
    /// <summary>
    /// Puts the car on the road where its rules say, after the road has moved this frame and before the camera looks at it. A
    /// sideswipe kicks the body; a crash ends the placing and breaks the car into its parts.
    /// </summary>
    internal sealed class CarPlacer : ILateTickable, IDisposable
    {
        private readonly CarView _view;
        private readonly IRoadFrame _road;
        private readonly ICarMotion _motion;
        private readonly IGameClock _clock;
        private readonly ILightLevels _levels;
        private readonly LightsConfig _lights;
        private readonly CrashConfig _crash;
        private readonly IRunSeed _seed;
        private readonly CarPoseRule _pose;
        private readonly IDisposable _subscriptions;

        public CarPlacer(CarView view, CarConfig config, IRoadFrame road, ICarMotion motion, IGameClock clock, ILightLevels levels, LightsConfig lights, CrashConfig crash, IRunSeed seed, ISubscriber<CarSideswiped> sideswipes, ISubscriber<CarCrashed> crashes)
        {
            _view = view;
            _road = road;
            _motion = motion;
            _clock = clock;
            _levels = levels;
            _lights = lights;
            _crash = crash;
            _seed = seed;
            _pose = new CarPoseRule(config.Settings);

            var bag = DisposableBag.CreateBuilder();
            sideswipes.Subscribe(OnSideswiped).AddTo(bag);
            crashes.Subscribe(OnCrashed).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        public void LateTick()
        {
            if (_view.IsWrecked)
            {
                return; // The parts are rigid bodies now.
            }

            // All four wheels on the road: on banked and steep pieces a car seated by the road's frame at its middle floats (WheelPoseRule).
            WheelPoseRule.At(_road.Path, _road.Distance, _motion.X, 1, _view.FrontAxle, _view.RearAxle, _view.HalfTrack, _road.Origin, out Vector3 position, out Quaternion rotation);
            _pose.Update(_motion.Velocity, _motion.Speed, _clock.DeltaTime);

            _view.Place(position, rotation * Quaternion.Euler(0f, _pose.YawDegrees, 0f));
            _view.Pose(_pose);
            _view.ShowSignal(_motion.Signal, _motion.LampOn);
            _view.ShowHeadlight(_levels.Headlights, _lights.Settings);
        }

        private void OnSideswiped(CarSideswiped swipe)
        {
            CrashSettings.SideswipeGroup group = _crash.Settings.Sideswipe;
            _pose.Kick(-swipe.Side, group.KickYawDegrees, group.KickRollDegrees, group.KickSeconds);
        }

        private void OnCrashed(CarCrashed crash)
        {
            var wreck = new WreckRule(_crash.Settings.Wreck, unchecked((int)_seed.Value ^ crash.CarId * 7919), crash.Speed, crash.Ahead, crash.Side);
            _view.Break(wreck, _crash.Settings.Wreck);
        }
    }
}
