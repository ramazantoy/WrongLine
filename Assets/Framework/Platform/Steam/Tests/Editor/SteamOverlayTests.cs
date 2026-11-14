using System;
using System.Collections.Generic;
using MessagePipe;
using NUnit.Framework;
using UnityEngine.InputSystem;
using VContainer;

namespace AF.Platform.Steam.Tests
{
    public class SteamOverlayTests : InputTestFixture
    {
        private IObjectResolver _container;
        private SteamOverlay _overlay;
        private List<bool> _messages;
        private IDisposable _subscription;
        private Gamepad _gamepad;
        private Keyboard _keyboard;
        private Mouse _mouse;

        [SetUp]
        public void SetUpOverlay()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<PlatformOverlayChanged>(messagePipe);
            builder.Register<SteamOverlay>(Lifetime.Singleton);
            _container = builder.Build();
            _overlay = _container.Resolve<SteamOverlay>();
            _messages = new List<bool>();
            _subscription = _container.Resolve<ISubscriber<PlatformOverlayChanged>>().Subscribe(change => _messages.Add(change.IsOpen));
            _gamepad = InputSystem.AddDevice<Gamepad>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
        }

        [TearDown]
        public void TearDownOverlay()
        {
            _overlay.Dispose();
            _subscription.Dispose();
            _container.Dispose();
        }

        [Test]
        public void SteamCallbacks_OpenAndClose_OnceForEachChange()
        {
            _overlay.OnActivated(true);
            _overlay.OnActivated(true);
            Assert.IsTrue(_overlay.IsOpen);

            _overlay.OnActivated(false);

            Assert.IsFalse(_overlay.IsOpen);
            CollectionAssert.AreEqual(new[] { true, false }, _messages);
        }

        [Test]
        public void OnSteamDeck_GamepadInputWhileOpen_ClosesItOnTheNextTick()
        {
            _overlay.TreatGameInputAsClosed = true;
            _overlay.OnActivated(true);

            Press(_gamepad.buttonSouth);
            Assert.IsTrue(_overlay.IsOpen, "Not inside the input update");
            _overlay.Tick();

            Assert.IsFalse(_overlay.IsOpen);
            // The lost "closed" arriving late changes nothing.
            _overlay.OnActivated(false);
            CollectionAssert.AreEqual(new[] { true, false }, _messages);
        }

        [Test]
        public void OnSteamDeck_LeftClickWhileOpen_ClosesIt()
        {
            _overlay.TreatGameInputAsClosed = true;
            _overlay.OnActivated(true);

            Press(_mouse.leftButton);
            _overlay.Tick();

            Assert.IsFalse(_overlay.IsOpen);
        }

        [Test]
        public void OnSteamDeck_KeyboardInput_DoesNotCloseIt()
        {
            _overlay.TreatGameInputAsClosed = true;
            _overlay.OnActivated(true);

            Press(_keyboard.spaceKey);
            _overlay.Tick();

            Assert.IsTrue(_overlay.IsOpen);
        }

        [Test]
        public void OffSteamDeck_GameInput_DoesNotCloseIt()
        {
            _overlay.OnActivated(true);

            Press(_gamepad.buttonSouth);
            _overlay.Tick();

            Assert.IsTrue(_overlay.IsOpen);
        }

        [Test]
        public void InputBeforeTheOverlayOpened_IsNotCarriedOver()
        {
            _overlay.TreatGameInputAsClosed = true;
            Press(_gamepad.buttonSouth);
            _overlay.OnActivated(true);

            _overlay.Tick();

            Assert.IsTrue(_overlay.IsOpen);
        }
    }
}
