using UnityEngine;

namespace WrongLine.World
{
    /// <summary>The day and night cycle's numbers. Read every frame, so it can be tuned while the game runs.</summary>
    [CreateAssetMenu(menuName = "WrongLine/Day Night Config")]
    public sealed class DayNightConfig : ScriptableObject
    {
        [SerializeField] private DayNightSettings _settings = new DayNightSettings();

        public DayNightSettings Settings => _settings;
    }
}
