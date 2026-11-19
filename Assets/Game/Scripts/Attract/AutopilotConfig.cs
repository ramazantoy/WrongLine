using UnityEngine;

namespace WrongLine.Attract
{
    /// <summary>The autopilot's numbers as an asset.</summary>
    [CreateAssetMenu(menuName = "WrongLine/Autopilot Config")]
    public sealed class AutopilotConfig : ScriptableObject
    {
        [SerializeField] private AutopilotSettings _settings = new AutopilotSettings();

        public AutopilotSettings Settings => _settings;
    }
}
