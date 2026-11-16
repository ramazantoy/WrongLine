using AF.Core;
using TMPro;
using UnityEngine;

namespace WrongLine.Driving
{
    /// <summary>The debug readout of the run (distance, piece, seed, speed, frame rate). Text is rewritten four times a second.</summary>
    public sealed class RoadDebugHud : MonoBehaviour, IRoadDebugHud
    {
        private const float RefreshSeconds = 0.25f;
        private const float FpsSmoothing = 0.1f;

        [SerializeField, Required] private TMP_Text _text;

        private float _smoothedFps = 60f;
        private float _sinceRefresh = float.MaxValue;

        public void Show(in RoadDebugInfo info)
        {
            float frame = Time.unscaledDeltaTime;
            if (frame > 0f)
            {
                _smoothedFps = Mathf.Lerp(_smoothedFps, 1f / frame, FpsSmoothing);
            }

            _sinceRefresh += frame;
            if (_sinceRefresh < RefreshSeconds)
            {
                return;
            }

            _sinceRefresh = 0f;
            _text.text = info.Format(_smoothedFps);
        }
    }
}
