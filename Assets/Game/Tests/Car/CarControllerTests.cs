using AF.Core;
using NUnit.Framework;
using UnityEngine;

namespace WrongLine.Car.Tests
{
    public class CarControllerTests
    {
        private sealed class FakeInput : ISteerInput
        {
            public float DragFraction { get; set; }

            public float Axis { get; set; }
        }

        private sealed class FakeClock : IGameClock
        {
            public float Time { get; set; }

            public float DeltaTime { get; set; } = 1f / 60f;

            public float Scale { get; set; } = 1f;
        }

        private CarConfig _config;
        private FakeInput _input;
        private FakeClock _clock;
        private CarController _car;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<CarConfig>();
            _input = new FakeInput();
            _clock = new FakeClock();
            _car = new CarController(_config, _input, _clock);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        private void Run(float seconds)
        {
            int frames = Mathf.RoundToInt(seconds / _clock.DeltaTime);
            for (int i = 0; i < frames; i++)
            {
                _car.Tick();
            }
        }

        [Test]
        public void TheCarPullsAwayToItsCruiseSpeed()
        {
            CarSettings settings = _config.Settings;
            Assert.AreEqual(settings.Speed.StartSpeed, _car.Speed);

            Run(10f);

            Assert.AreEqual(settings.Speed.CruiseSpeed, _car.Speed);
        }

        [Test]
        public void ADragToTheRight_MovesTheCarRight_AndSignalsUntilItHasSettledInTheNextLane()
        {
            _input.DragFraction = 0.25f; // 3 m with the default settings: the right lane.
            _car.Tick();
            _input.DragFraction = 0f;

            Assert.AreEqual(SignalDirection.Right, _car.Signal);
            Assert.IsTrue(_car.LampOn);
            Assert.Greater(_car.TargetX, 2.5f);

            Run(0.6f);
            Assert.AreEqual(1, _car.Lane);
            Assert.AreEqual(_car.TargetX, _car.X, 0.05f);
            Assert.AreEqual(SignalDirection.Right, _car.Signal, "Still blinking: a signal always blinks its minimum.");

            Run(3f);
            Assert.AreEqual(SignalDirection.None, _car.Signal);
            Assert.IsFalse(_car.LampOn);
        }

        [Test]
        public void AHeldKey_MovesTheCarLeft()
        {
            _input.Axis = -1f;

            Run(1f);

            Assert.Less(_car.X, -2f);
            Assert.AreEqual(-1, _car.Lane);
            Assert.AreEqual(-_config.Settings.LateralLimit, _car.TargetX, 1e-4f);
        }

        [Test]
        public void APause_ChangesNothing()
        {
            _input.DragFraction = 0.25f;
            _clock.DeltaTime = 0f;

            for (int i = 0; i < 100; i++)
            {
                _car.Tick();
            }

            Assert.AreEqual(0f, _car.X);
            Assert.AreEqual(_config.Settings.Speed.StartSpeed, _car.Speed);
        }
    }
}
