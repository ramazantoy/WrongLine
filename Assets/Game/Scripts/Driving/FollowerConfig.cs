using UnityEngine;

namespace WrongLine.Driving
{
    /// <summary>How the dummy follower moves. Read every frame, so it can be changed in the Inspector while the game runs.</summary>
    [CreateAssetMenu(menuName = "WrongLine/Follower Config")]
    public sealed class FollowerConfig : ScriptableObject
    {
        [Tooltip("Speed along the road, in metres per second (30 is 108 km/h).")]
        [SerializeField, Min(0f)] private float _speedMetersPerSecond = 30f;

        public float SpeedMetersPerSecond => _speedMetersPerSecond;
    }
}
