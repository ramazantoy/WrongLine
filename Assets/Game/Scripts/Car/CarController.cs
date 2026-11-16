using AF.Core;
using VContainer.Unity;

namespace WrongLine.Car
{
    /// <summary>Runs the car's rules each frame from the player's input. The road run and the views read the result through <see cref="ICarMotion"/>.</summary>
    internal sealed class CarController : ITickable, ICarMotion
    {
        private readonly ISteerInput _input;
        private readonly IGameClock _clock;
        private readonly SpeedRule _speed;
        private readonly LateralRule _lateral;
        private readonly TurnSignalRule _signal;

        public CarController(CarConfig config, ISteerInput input, IGameClock clock)
        {
            _input = input;
            _clock = clock;
            // The rules read the config's settings object every frame, so tuning it in the Inspector shows immediately.
            _speed = new SpeedRule(config.Settings);
            _lateral = new LateralRule(config.Settings);
            _signal = new TurnSignalRule(config.Settings);
        }

        public float Speed => _speed.Speed;

        public float X => _lateral.X;

        public float TargetX => _lateral.TargetX;

        public float Velocity => _lateral.Velocity;

        public int Lane => _signal.CarLane;

        public SignalDirection Signal => _signal.Direction;

        public bool LampOn => _signal.LampOn;

        public void Tick()
        {
            float deltaTime = _clock.DeltaTime;
            _speed.Update(deltaTime);
            _lateral.Update(_input.DragFraction, _input.Axis, deltaTime);
            _signal.Update(_lateral.X, _lateral.TargetX, deltaTime);
        }
    }
}
