using UnityEngine;

namespace WrongLine.Driving
{
    /// <summary>Sits on the scene camera; the driver tells it where to be and how wide to look.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class ChaseCameraView : MonoBehaviour
    {
        private Camera _camera;

        public Vector3 Position => transform.position;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        public void Apply(Vector3 position, Quaternion rotation, float fieldOfView)
        {
            transform.SetPositionAndRotation(position, rotation);
            _camera.fieldOfView = fieldOfView;
        }
    }
}
