using UnityEngine;

namespace WrongLine.Driving
{
    /// <summary>The chase camera's numbers. Read every frame, so it can be tuned while the game runs.</summary>
    [CreateAssetMenu(menuName = "WrongLine/Chase Camera Config")]
    public sealed class ChaseCameraConfig : ScriptableObject
    {
        [SerializeField] private ChaseCameraSettings _settings = new ChaseCameraSettings();

        public ChaseCameraSettings Settings => _settings;
    }
}
