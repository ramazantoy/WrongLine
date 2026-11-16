using UnityEngine;
using UnityEngine.InputSystem;

namespace WrongLine.Car
{
    /// <summary>Reads the Gameplay map's Drag, Press and Move actions. The input mode service decides whether the map is on.</summary>
    internal sealed class SteerInput : ISteerInput
    {
        private readonly InputAction _drag;
        private readonly InputAction _press;
        private readonly InputAction _move;

        public SteerInput(InputActionAsset actions)
        {
            _drag = actions.FindAction("Gameplay/Drag", throwIfNotFound: true);
            _press = actions.FindAction("Gameplay/Press", throwIfNotFound: true);
            _move = actions.FindAction("Gameplay/Move", throwIfNotFound: true);
        }

        public float DragFraction => _press.IsPressed() && Screen.width > 0 ? _drag.ReadValue<Vector2>().x / Screen.width : 0f;

        public float Axis => Mathf.Clamp(_move.ReadValue<Vector2>().x, -1f, 1f);
    }
}
