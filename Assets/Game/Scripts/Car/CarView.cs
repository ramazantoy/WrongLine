using AF.Core;
using UnityEngine;
using WrongLine.Crash;
using WrongLine.World;

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

        [Tooltip("The two white headlights: spot lights shining ahead, lit at night.")]
        [SerializeField, Required] private Light _headlight;

        [SerializeField, Required] private Light _headlightRight;

        [Tooltip("A soft point light at the nose, beside the beams.")]
        [SerializeField, Required] private Light _headlightGlow;

        [Tooltip("One red point light at the rear, lit with the headlights.")]
        [SerializeField, Required] private Light _tailLight;

        [Tooltip("The amber lights of the left and right turn signal (point lights), blinking with the signal lamps.")]
        [SerializeField, Required] private Light _signalLightLeft;

        [SerializeField, Required] private Light _signalLightRight;

        private Quaternion _wheelRestFrontLeft;
        private Quaternion _wheelRestFrontRight;
        private Quaternion _wheelRestRearLeft;
        private Quaternion _wheelRestRearRight;
        private Quaternion _steeringRest;
        private Vector3 _steeringAxisNormalized;

        /// <summary>Where the wheels touch the road, from the wheel nodes: the axles along the car's forward axis and half the track (see <see cref="WrongLine.Road.WheelPoseRule"/>).</summary>
        public float FrontAxle { get; private set; }

        public float RearAxle { get; private set; }

        public float HalfTrack { get; private set; }

        private void Awake()
        {
            Vector3 frontLeft = transform.InverseTransformPoint(_wheelFrontLeft.position);
            Vector3 frontRight = transform.InverseTransformPoint(_wheelFrontRight.position);
            Vector3 rearLeft = transform.InverseTransformPoint(_wheelRearLeft.position);
            Vector3 rearRight = transform.InverseTransformPoint(_wheelRearRight.position);
            FrontAxle = (frontLeft.z + frontRight.z) * 0.5f;
            RearAxle = (rearLeft.z + rearRight.z) * 0.5f;
            HalfTrack = Mathf.Abs(frontRight.x - frontLeft.x) * 0.5f;

            _wheelRestFrontLeft = _wheelFrontLeft.localRotation;
            _wheelRestFrontRight = _wheelFrontRight.localRotation;
            _wheelRestRearLeft = _wheelRearLeft.localRotation;
            _wheelRestRearRight = _wheelRearRight.localRotation;
            _steeringRest = _steeringWheel.localRotation;
            _steeringAxisNormalized = _steeringAxis.normalized;
        }

        /// <summary>The wreck's middle once the car has broken up (see <see cref="Break"/>), null before.</summary>
        public Transform WreckBody { get; private set; }

        public bool IsWrecked => WreckBody != null;

        public void Place(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
        }

        /// <summary>The car breaks up: lights out, the parts become rigid bodies and fly off (see <see cref="CarWreck"/>).</summary>
        public void Break(WreckRule wreck, CrashSettings.WreckGroup settings)
        {
            if (IsWrecked)
            {
                return;
            }

            ShowSignal(SignalDirection.None, false, null);
            foreach (Light light in new[] { _headlight, _headlightRight, _headlightGlow, _tailLight })
            {
                light.enabled = false;
            }

            WreckBody = CarWreck.Break(transform, wreck, settings);
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

        public void ShowSignal(SignalDirection direction, bool lampOn, LightsSettings lights)
        {
            bool left = direction == SignalDirection.Left && lampOn;
            bool right = direction == SignalDirection.Right && lampOn;
            SetActive(_leftLamps, left);
            SetActive(_rightLamps, right);
            ShowSignalLight(_signalLightLeft, left, lights);
            ShowSignalLight(_signalLightRight, right, lights);
        }

        /// <summary>Lights the headlight to <paramref name="level"/> (0 off, 1 full); off lights are disabled, so they cost the renderer nothing.</summary>
        public void ShowHeadlight(float level, LightsSettings lights)
        {
            ShowLamp(_headlight, level, lights.HeadlightColor, lights.HeadlightIntensity, lights.HeadlightRange, lights.HeadlightSpotAngle);
            ShowLamp(_headlightRight, level, lights.HeadlightColor, lights.HeadlightIntensity, lights.HeadlightRange, lights.HeadlightSpotAngle);
            ShowLamp(_headlightGlow, level, lights.HeadlightGlowColor, lights.HeadlightGlowIntensity, lights.HeadlightGlowRange, 0f);
            ShowLamp(_tailLight, level, lights.TailColor, lights.TailIntensity, lights.TailRange, 0f);
        }

        private static void ShowLamp(Light light, float level, Color color, float intensity, float range, float spotAngle)
        {
            light.enabled = level > 0.001f;
            light.color = color;
            light.intensity = intensity * level;
            light.range = range;
            if (spotAngle > 0f)
            {
                light.spotAngle = spotAngle;
            }
        }

        private static void ShowSignalLight(Light light, bool on, LightsSettings lights)
        {
            light.enabled = on;
            if (on)
            {
                light.color = lights.SignalColor;
                light.intensity = lights.SignalIntensity;
                light.range = lights.SignalRange;
            }
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
