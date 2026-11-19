using VContainer.Unity;
using WrongLine.Car;
using WrongLine.Road;
using WrongLine.Traffic;

namespace WrongLine.Attract
{
    /// <summary>
    /// Each frame sets the car's steering from <see cref="AutopilotRule"/>. It is not given to the car's controller as its input
    /// (the controller is built before the traffic, which is built from the road, which reads the controller): it writes into
    /// <see cref="AutopilotSteerInput"/>, which the controller reads on its next tick.
    /// </summary>
    internal sealed class AutopilotDriver : ITickable
    {
        private readonly AutopilotSteerInput _input;
        private readonly ICarMotion _motion;
        private readonly ITrafficFrame _traffic;
        private readonly AutopilotRule _rule;

        public AutopilotDriver(AutopilotSteerInput input, ICarMotion motion, ITrafficFrame traffic, AutopilotConfig config)
        {
            _input = input;
            _motion = motion;
            _traffic = traffic;
            // The rule reads the config's settings object every frame, so tuning it in the Inspector shows immediately.
            _rule = new AutopilotRule(config.Settings);
        }

        public void Tick()
        {
            TrafficCorridor corridor = _traffic.Corridor;
            if (corridor == null || _motion.Crashed)
            {
                _input.Axis = 0f; // No traffic (switched off): the car stays in its lane.
                return;
            }

            float desiredX = RoadSection.LaneCenterX(_rule.LaneAt(corridor, _motion.Distance));
            _input.Axis = _rule.Axis(desiredX, _motion.TargetX);
        }
    }
}
