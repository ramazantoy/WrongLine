using UnityEngine;

namespace WrongLine.Crash
{
    /// <summary>Collision, crash and game over numbers. Read every frame, so it can be tuned while the game runs.</summary>
    [CreateAssetMenu(menuName = "WrongLine/Crash Config")]
    public sealed class CrashConfig : ScriptableObject
    {
        [SerializeField] private CrashSettings _settings = new CrashSettings();

        public CrashSettings Settings => _settings;
    }
}
