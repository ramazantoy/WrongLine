using UnityEngine;

namespace WrongLine.Traffic
{
    /// <summary>Every tunable number of the traffic. Read at the start of a run; edit it in the Inspector between runs.</summary>
    [CreateAssetMenu(menuName = "WrongLine/Traffic Config")]
    public sealed class TrafficConfig : ScriptableObject
    {
        [Tooltip("Turn the traffic off without taking it out of the scene.")]
        [SerializeField] private bool _enabled = true;

        [SerializeField] private TrafficSettings _settings = new TrafficSettings();

        public bool Enabled => _enabled;

        public TrafficSettings Settings => _settings;
    }
}
