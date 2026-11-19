using UnityEngine;

namespace WrongLine.UI
{
    /// <summary>The in-game screens' numbers: how long the "new record" label stays and how the game over distance counts up.</summary>
    [CreateAssetMenu(menuName = "WrongLine/UI Config")]
    public sealed class UiConfig : ScriptableObject
    {
        [Tooltip("How long the HUD's new record label stays up once the run passes the best.")]
        [SerializeField, Min(0.5f)] private float _recordLabelSeconds = 2.5f;

        [Tooltip("How long the game over distance takes to count from 0 up to the run's distance.")]
        [SerializeField, Min(0f)] private float _countUpSeconds = 1.4f;

        [Tooltip("How long the game over screen waits before it starts counting (it fades in meanwhile).")]
        [SerializeField, Min(0f)] private float _countUpDelay = 0.35f;

        public float RecordLabelSeconds => _recordLabelSeconds;

        public float CountUpSeconds => _countUpSeconds;

        public float CountUpDelay => _countUpDelay;
    }
}
