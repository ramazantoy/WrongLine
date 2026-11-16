using UnityEngine;

namespace WrongLine.Driving
{
    /// <summary>Sits on the scene camera; the driver tells it where to be.</summary>
    public sealed class ChaseCameraView : MonoBehaviour
    {
        public void Apply(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
