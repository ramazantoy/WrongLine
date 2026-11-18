using AF.Core;
using MessagePipe;
using VContainer.Unity;
using WrongLine.Car;
using WrongLine.Driving;
using WrongLine.Road;
using WrongLine.Traffic;

namespace WrongLine.Crash
{
    /// <summary>
    /// Each frame, asks <see cref="CollisionRule"/> whether the player's car touched a traffic car and tells the car and the rest
    /// of the game: a sideswipe pushes the car and is announced (<see cref="CarSideswiped"/>), a crash stops it and ends the run
    /// (<see cref="CarCrashed"/>). Runs after the road and the traffic have moved this frame.
    /// </summary>
    internal sealed class CrashDetector : IStartable, ITickable
    {
        private readonly IRoadFrame _road;
        private readonly ICarMotion _motion;
        private readonly ICarImpact _impact;
        private readonly ITrafficFrame _traffic;
        private readonly ITrafficPool _pool;
        private readonly CrashConfig _config;
        private readonly TrafficConfig _trafficConfig;
        private readonly IPublisher<CarSideswiped> _sideswiped;
        private readonly IPublisher<CarCrashed> _crashed;
        private CollisionRule _rule;
        private PlayerBox _last;
        private bool _hasLast;
        private bool _done;
        private int _sideswipes;

        public CrashDetector(IRoadFrame road, ICarMotion motion, ICarImpact impact, ITrafficFrame traffic, ITrafficPool pool, CrashConfig config, TrafficConfig trafficConfig, IPublisher<CarSideswiped> sideswiped, IPublisher<CarCrashed> crashed)
        {
            _road = road;
            _motion = motion;
            _impact = impact;
            _traffic = traffic;
            _pool = pool;
            _config = config;
            _trafficConfig = trafficConfig;
            _sideswiped = sideswiped;
            _crashed = crashed;
        }

        public void Start()
        {
            _config.Settings.Validate(_trafficConfig.Settings.Layout.PlayerLengthMeters);
            _rule = new CollisionRule(_config.Settings);
        }

        public void Tick()
        {
            if (_done || _rule == null)
            {
                return;
            }

            var now = new PlayerBox(_traffic.Time, _road.Distance, _motion.X);
            PlayerBox from = _hasLast ? _last : now;
            _last = now;
            _hasLast = true;

            double ratio = WheelPoseRule.LaneLengthRatio(_road.Path, now.Distance, now.X, _road.Origin);
            CollisionResult result = _rule.Update(from, now, _traffic.Active, _pool.Models, ratio);
            switch (result.Kind)
            {
                case CollisionKind.Sideswipe:
                    CrashSettings.SideswipeGroup swipe = _config.Settings.Sideswipe;
                    _impact.Sideswipe(-result.Side, swipe.PushMeters, swipe.PushSpeed);
                    _sideswipes++;
                    Log.Info($"Sideswipe with car {result.CarId}, {result.Depth:F2} m deep (#{_sideswipes}).");
                    _sideswiped.Publish(new CarSideswiped(result.CarId, result.Side, result.Depth, _sideswipes));
                    break;

                case CollisionKind.Crash:
                    _done = true;
                    float speed = _motion.Speed; // Before the crash stops the car.
                    _impact.Crash();
                    Log.Info($"Crash with car {result.CarId} at {result.ClosingSpeed:F1} m/s (own speed {speed:F1}).");
                    _crashed.Publish(new CarCrashed(result.CarId, result.Side, result.ClosingSpeed, speed, result.Ahead, now.Distance));
                    break;
            }
        }
    }
}
