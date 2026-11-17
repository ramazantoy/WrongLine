using UnityEngine;

namespace WrongLine.World
{
    /// <summary>The night lights' numbers. Read every frame, so it can be tuned while the game runs.</summary>
    [CreateAssetMenu(menuName = "WrongLine/Lights Config")]
    public sealed class LightsConfig : ScriptableObject
    {
        [SerializeField] private LightsSettings _settings = new LightsSettings();

        public LightsSettings Settings => _settings;
    }
}
