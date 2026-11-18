using AF.Core;
using NUnit.Framework;
using UnityEngine;

namespace WrongLine.Car.Tests
{
    public class CarControllerCrashTests
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
        private CarController _car;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<CarConfig>();
            _input = new FakeInput();
            _car = new CarController(_config, _input, new FakeClock());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        private void Run(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                _car.Tick();
            }
        }

        [Test]
        public void ASideswipePushesTheCarSidewaysAndLeavesItsSpeedAlone()
        {
            Run(60);
            float speed = _car.Speed;
            float x = _car.X;

            _car.Sideswipe(-1, 0.8f, 5f);
            Run(1);

            Assert.Less(_car.X, x, "pushed to the left");
            Assert.AreEqual(speed, _car.Speed, speed * 0.05f, "the next frame's own acceleration is all that changes it");
            Assert.IsFalse(_car.Crashed);
        }

        [Test]
        public void ACrashStopsTheCarAndItsSidewaysMovement()
        {
            Run(30);
            _input.Axis = 1f;
            _car.Crash();
            float x = _car.X;

            Run(120);

            Assert.IsTrue(_car.Crashed);
            Assert.AreEqual(0f, _car.Speed);
            Assert.AreEqual(0f, _car.Velocity);
            Assert.AreEqual(x, _car.X, "the held key moves nothing any more");
            Assert.AreEqual(SignalDirection.None, _car.Signal);
            Assert.IsFalse(_car.LampOn);
        }

        [Test]
        public void ASideswipeAfterACrashDoesNothing()
        {
            _car.Crash();
            float x = _car.X;
            float target = _car.TargetX;

            _car.Sideswipe(1, 3f, 10f);

            Assert.AreEqual(x, _car.X);
            Assert.AreEqual(target, _car.TargetX);
        }
    }
}
