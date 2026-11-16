using UnityEngine;

namespace WrongLine.Driving
{
    /// <summary>The visible stand-in that rides the road centre line: a box, placed by the streamer each frame.</summary>
    public sealed class DummyFollowerView : MonoBehaviour
    {
        public Vector3 Position => transform.position;

        public Quaternion Rotation => transform.rotation;

        public void Place(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
