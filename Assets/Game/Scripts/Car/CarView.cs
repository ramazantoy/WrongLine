using AF.Core;
using UnityEngine;
using WrongLine.Audio;
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

        [Tooltip("Lamp objects shown with the left and right signal (placeholder cubes). Empty when the model lights its own signal panels through the light mask.")]
        [SerializeField] private GameObject[] _leftLamps = new GameObject[0];

        [SerializeField] private GameObject[] _rightLamps = new GameObject[0];

        [Tooltip("The body's renderer: its light mask glows with the lights (headlight and stop panels with the headlights, signal panels with the signals).")]
        [SerializeField, Required] private Renderer _bodyRenderer;

        [Tooltip("The two white headlights: spot lights shining ahead, lit at night.")]
        [SerializeField, Required] private Light _headlight;

        [SerializeField, Required] private Light _headlightRight;

        [Tooltip("A soft point light at the nose, beside the beams.")]
        [SerializeField, Required] private Light _headlightGlow;

        [Tooltip("The car's engine and road loops and the cues of what happens around it.")]
        [SerializeField, Required] private CarAudioView _audio;

        private static readonly int _lightLevelsId = Shader.PropertyToID("_CarLightLevels");

        private MaterialPropertyBlock _block;
        private Vector4 _glow;
        private Vector4 _glowApplied;
        private Quaternion _wheelRestFrontLeft;
        private Quaternion _wheelRestFrontRight;
        private Quaternion _wheelRestRearLeft;
        private Quaternion _wheelRestRearRight;
        private Quaternion _steeringRest;
        private Vector3 _steeringAxisNormalized;

        /// <summary>Where the wheels touch the road, from the wheel nodes: the axles along the car's forward axis and half the track (see <see cref="WrongLine.Road.WheelPoseRule"/>).</summary>
        public CarAudioView Audio => _audio;

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

        /// <summary>Where the car's body is now: on the road, turned as the car heads and leaning as it rolls (for a camera that rides in it).</summary>
        public void BodyPose(out Vector3 position, out Quaternion rotation)
        {
            position = _chassis.position;
            rotation = _chassis.rotation;
        }

        /// <summary>The car breaks up: lights out, the parts become rigid bodies and fly off (see <see cref="CarWreck"/>).</summary>
        public void Break(WreckRule wreck, CrashSettings.WreckGroup settings)
        {
            if (IsWrecked)
            {
                return;
            }

            ShowSignal(SignalDirection.None, false);
            foreach (Light light in new[] { _headlight, _headlightRight, _headlightGlow })
            {
                light.enabled = false;
            }

            _glow = Vector4.zero;
            ApplyGlow();
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

        public void ShowSignal(SignalDirection direction, bool lampOn)
        {
            bool left = direction == SignalDirection.Left && lampOn;
            bool right = direction == SignalDirection.Right && lampOn;
            SetActive(_leftLamps, left);
            SetActive(_rightLamps, right);
            _glow.z = left ? 1f : 0f;
            _glow.w = right ? 1f : 0f;
            ApplyGlow();
        }

        /// <summary>Writes the light levels to the body's renderer (a property block: the material is shared), only when they changed.</summary>
        private void ApplyGlow()
        {
            if (_glow == _glowApplied)
            {
                return;
            }

            _glowApplied = _glow;
            _block ??= new MaterialPropertyBlock();
            _bodyRenderer.GetPropertyBlock(_block);
            _block.SetVector(_lightLevelsId, _glow);
            _bodyRenderer.SetPropertyBlock(_block);
        }

        /// <summary>Lights the headlight to <paramref name="level"/> (0 off, 1 full); off lights are disabled, so they cost the renderer nothing.</summary>
        public void ShowHeadlight(float level, LightsSettings lights)
        {
            ShowLamp(_headlight, level, lights.HeadlightColor, lights.HeadlightIntensity, lights.HeadlightRange, lights.HeadlightSpotAngle);
            ShowLamp(_headlightRight, level, lights.HeadlightColor, lights.HeadlightIntensity, lights.HeadlightRange, lights.HeadlightSpotAngle);
            ShowLamp(_headlightGlow, level, lights.HeadlightGlowColor, lights.HeadlightGlowIntensity, lights.HeadlightGlowRange, 0f);
            _glow.x = level; // The headlight panels.
            _glow.y = level; // The stop panels (the rear lights glow on the model, no light is cast): lit with the headlights (there is no brake yet).
            ApplyGlow();
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
