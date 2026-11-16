using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace WrongLine.Car.Tests
{
    /// <summary>The real Controls.inputactions through <see cref="SteerInput"/>, with virtual devices: the map's bindings are what is under test.</summary>
    public class SteerInputTests : InputTestFixture
    {
        private InputActionAsset _actions;
        private Mouse _mouse;
        private Keyboard _keyboard;
        private SteerInput _input;

        [SetUp]
        public void SetUpActions()
        {
            _mouse = InputSystem.AddDevice<Mouse>();
            _keyboard = InputSystem.AddDevice<Keyboard>();

            // A copy of the imported asset, so the test never touches the shared one.
            var source = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Game/Input/Controls.inputactions");
            _actions = InputActionAsset.FromJson(source.ToJson());
            _actions.FindActionMap("Gameplay", true).Enable();
            _input = new SteerInput(_actions);
        }

        [TearDown]
        public void TearDownActions()
        {
            Object.DestroyImmediate(_actions);
        }

        private void MoveMouse(float deltaX)
        {
            InputSystem.QueueStateEvent(_mouse, new MouseState { delta = new Vector2(deltaX, 0f), buttons = (ushort)(_mouse.leftButton.isPressed ? 1 : 0) });
            InputSystem.Update();
        }

        [Test]
        public void HoldingTheLeftButtonAndMoving_IsADragAsAFractionOfTheScreenWidth()
        {
            Press(_mouse.leftButton);

            MoveMouse(Screen.width * 0.25f);

            Assert.AreEqual(0.25f, _input.DragFraction, 1e-3f);
        }

        [Test]
        public void MovingTheMouseWithoutTheButton_IsNotADrag()
        {
            MoveMouse(Screen.width * 0.25f);

            Assert.AreEqual(0f, _input.DragFraction);
        }

        [Test]
        public void DraggingLeft_IsNegative()
        {
            Press(_mouse.leftButton);

            MoveMouse(-Screen.width * 0.1f);

            Assert.AreEqual(-0.1f, _input.DragFraction, 1e-3f);
        }

        [Test]
        public void ReleasingTheButton_EndsTheDrag()
        {
            Press(_mouse.leftButton);
            MoveMouse(Screen.width * 0.25f);
            Release(_mouse.leftButton);

            MoveMouse(Screen.width * 0.25f);

            Assert.AreEqual(0f, _input.DragFraction);
        }

        [Test]
        public void TheKeys_GiveTheAxis()
        {
            Assert.AreEqual(0f, _input.Axis);

            Press(_keyboard.dKey);
            Assert.AreEqual(1f, _input.Axis);
            Release(_keyboard.dKey);

            Press(_keyboard.leftArrowKey);
            Assert.AreEqual(-1f, _input.Axis);
        }

        [Test]
        public void WithTheMapOff_NothingIsRead()
        {
            _actions.FindActionMap("Gameplay", true).Disable();
            Press(_mouse.leftButton);
            MoveMouse(Screen.width * 0.25f);
            Press(_keyboard.dKey);

            Assert.AreEqual(0f, _input.DragFraction);
            Assert.AreEqual(0f, _input.Axis);
        }
    }
}
