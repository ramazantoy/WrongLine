using UnityEngine;

namespace WrongLine.Attract
{
    /// <summary>The first-person camera's numbers as an asset.</summary>
    [CreateAssetMenu(menuName = "WrongLine/First Person Camera Config")]
    public sealed class FirstPersonCameraConfig : ScriptableObject
    {
        [SerializeField] private FirstPersonCameraSettings _settings = new FirstPersonCameraSettings();

        public FirstPersonCameraSettings Settings => _settings;
    }
}
