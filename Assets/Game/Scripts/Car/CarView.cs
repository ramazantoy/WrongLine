using AF.Core;
using UnityEngine;

namespace WrongLine.Car
{
    /// <summary>
    /// The car's scene objects: places the car on the road and turns the parts the rules describe (body lean, steering wheel,
    /// front wheels, wheel spin, turn signal lamps). The numbers come from <see cref="CarPoseRule"/> and <see cref="TurnSignalRule"/>.
    /// </summary>
    public sealed class CarView : MonoBehaviour
    {
        [SerializeField, Required] private Transform _chassis;
        [SerializeField, Required] private Transform _steeringWheel;

        [Tooltip("The steering wheel's turning axis, in its own local space (the column).")]
        [SerializeField] private Vector3 _steeringAxis = Vector3.forward;

        [SerializeField, Required] private Transform _wheelFrontLeft;
        [SerializeField, Required] private Transform _wheelFrontRight;
        [SerializeField, Required] private Transform _wheelRearLeft;
        [SerializeField, Required] private Transform _wheelRearRight;

        [SerializeField, Required] private GameObject[] _leftLamps;
        [SerializeField, Required] private GameObject[] _rightLamps;

        private Quaternion _wheelRestFrontLeft;
        private Quaternion _wheelRestFrontRight;
        private Quaternion _wheelRestRearLeft;
        private Quaternion _wheelRestRearRight;
        private Quaternion _steeringRest;
        private Vector3 _steeringAxisNormalized;

        private void Awake()
        {
            _wheelRestFrontLeft = _wheelFrontLeft.localRotation;
            _wheelRestFrontRight = _wheelFrontRight.localRotation;
            _wheelRestRearLeft = _wheelRearLeft.localRotation;
            _wheelRestRearRight = _wheelRearRight.localRotation;
            _steeringRest = _steeringWheel.localRotation;
            _steeringAxisNormalized = _steeringAxis.normalized;
        }

        public void Place(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
        }

        public void Pose(CarPoseRule pose)
        {
            _chassis.localRotation = Quaternion.Euler(0f, 0f, pose.RollDegrees);
            _steeringWheel.localRotation = _steeringRest * Quaternion.AngleAxis(pose.SteeringWheelDegrees, _steeringAxisNormalized);

            Quaternion steer = Quaternion.Euler(0f, pose.FrontWheelSteerDegrees, 0f);
            Quaternion spin = Quaternion.Euler(pose.WheelSpinDegrees, 0f, 0f);
            _wheelFrontLeft.localRotation = steer * _wheelRestFrontLeft * spin;
            _wheelFrontRight.localRotation = steer * _wheelRestFrontRight * spin;
            _wheelRearLeft.localRotation = _wheelRestRearLeft * spin;
            _wheelRearRight.localRotation = _wheelRestRearRight * spin;
        }

        public void ShowSignal(SignalDirection direction, bool lampOn)
        {
            SetActive(_leftLamps, direction == SignalDirection.Left && lampOn);
            SetActive(_rightLamps, direction == SignalDirection.Right && lampOn);
        }

        private static void SetActive(GameObject[] lamps, bool active)
        {
            foreach (GameObject lamp in lamps)
            {
                if (lamp.activeSelf != active)
                {
                    lamp.SetActive(active);
                }
            }
        }
    }
}
