using UnityEngine;

namespace WrongLine.World
{
    /// <summary>The world bend's numbers. Read every frame, so it can be tuned while the game runs.</summary>
    [CreateAssetMenu(menuName = "WrongLine/Curve World Config")]
    public sealed class CurveWorldConfig : ScriptableObject
    {
        [SerializeField] private CurveWorldSettings _settings = new CurveWorldSettings();

        public CurveWorldSettings Settings => _settings;
    }
}
