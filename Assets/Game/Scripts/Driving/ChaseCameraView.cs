using AF.Core;
using PrimeTween;
using Unity.Cinemachine;
using UnityEngine;

namespace WrongLine.Driving
{
    /// <summary>
    /// The scene camera: a Cinemachine brain on the camera and two virtual cameras that follow one rig object, which the driver
    /// puts where the camera rule says (position and look in one pose). The chase camera is the one in use; at a crash the crash
    /// camera (a tighter view of the same rig) takes over with a blend. The camera shake is a PrimeTween shake of a child of the rig
    /// (a smooth, fading jolt; Cinemachine's impulse noise looked fake).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class ChaseCameraView : MonoBehaviour
    {
        private const int ChasePriority = 10;
        private const int CrashPriority = 20;

        /// <summary>Shakes per second: low enough to read as a jolt, not as a buzz.</summary>
        private const float ShakeFrequency = 10f;

        [SerializeField, Required] private CinemachineBrain _brain;
        [SerializeField, Required] private CinemachineCamera _chase;
        [SerializeField, Required] private CinemachineCamera _crash;

        [Tooltip("Moved by the driver every frame to where the camera should be and look.")]
        [SerializeField, Required] private Transform _rig;

        [Tooltip("A child of the rig that both virtual cameras follow; the shake moves it a little in the rig's own space.")]
        [SerializeField, Required] private Transform _shake;

        /// <summary>Where the camera is now, as the brain put it this frame (read after the brain has run).</summary>
        public Vector3 Position => transform.position;

        /// <summary>Puts the rig where the camera should be and look, and sets the chase camera's field of view.</summary>
        public void Apply(Vector3 position, Quaternion rotation, float fieldOfView)
        {
            _rig.SetPositionAndRotation(position, rotation);
            _chase.Lens.FieldOfView = fieldOfView;
        }

        /// <summary>Cuts to the crash camera (a tighter view of the same rig) with a blend of <paramref name="blendSeconds"/>.</summary>
        public void ShowCrashView(float fieldOfView, float blendSeconds)
        {
            _crash.Lens.FieldOfView = fieldOfView;
            _brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blendSeconds);
            _crash.Priority.Value = CrashPriority;
        }

        /// <summary>Back to the chase camera at once (a new run starts with the scene, so this is for completeness).</summary>
        public void ShowChaseView()
        {
            _crash.Priority.Value = ChasePriority - 1;
        }

        /// <summary>
        /// Shakes the camera sideways and up and down, <paramref name="meters"/> at the most, fading out over <paramref name="seconds"/> of
        /// real time (the crash slows the game down; the shake should not). Zero does nothing.
        /// </summary>
        public void Shake(float meters, float seconds)
        {
            if (meters <= 0f)
            {
                return;
            }

            Tween.StopAll(_shake);
            Tween.ShakeLocalPosition(_shake, new Vector3(meters, meters * 0.7f, 0f), seconds, frequency: ShakeFrequency, useUnscaledTime: true);
        }
    }
}
