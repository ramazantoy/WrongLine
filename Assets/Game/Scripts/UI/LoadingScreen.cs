using System.Threading;
using AF.Core;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WrongLine.UI
{
    /// <summary>
    /// The scene change cover, on the root prefab so it outlives every scene: it fades in, the road fills from empty to full at a
    /// steady speed and starts over, again and again until the next scene is ready, and it fades away. All in unscaled time; the game
    /// may be paused while it loads. The road does not follow the load's progress (it would only be a fake bar); the cover stays up
    /// long enough to be seen. The change that leaves the intro is not covered (<see cref="LoadingCoverRule"/>).
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour, ILoadingScreen
    {
        [Tooltip("The whole screen. Shown and hidden; this component's object stays active.")]
        [SerializeField, Required] private GameObject _root;

        [SerializeField, Required] private CanvasGroup _group;

        [Tooltip("The road: a vertically filled sprite, filled from its fill origin.")]
        [FormerlySerializedAs("_roadBottom")]
        [SerializeField, Required] private Image _road;

        [Tooltip("Seconds one fill from empty to full takes; the speed is constant, then it starts over.")]
        [SerializeField, Min(0.1f)] private float _secondsPerFill = 1.1f;

        [Tooltip("The cover stays up at least this long (from the moment it starts to show), however fast the scene loads: a quick load would only flash it.")]
        [SerializeField, Min(0f)] private float _minShownSeconds = 5f;

        [SerializeField, Min(0.05f)] private float _fadeSeconds = 0.25f;

        private float _shownAt;
        private bool _covering;

        private void Awake() => _root.SetActive(false);

        public async UniTask Show(CancellationToken cancellation)
        {
            // The scene being left is still the active one while the cover goes up.
            _covering = LoadingCoverRule.ShouldCover(SceneManager.GetActiveScene().name);
            if (!_covering)
            {
                return;
            }

            _shownAt = Time.unscaledTime;
            _road.fillAmount = 0f;
            _group.alpha = 0f;
            _group.blocksRaycasts = true;
            _root.SetActive(true);
            await Tween.Alpha(_group, 1f, _fadeSeconds, useUnscaledTime: true).ToUniTask(cancellation);
        }

        public void SetProgress(float progress)
        {
            // Not shown: the road runs on its own.
        }

        public async UniTask Hide(CancellationToken cancellation)
        {
            if (!_covering)
            {
                return;
            }

            _covering = false;

            while (Time.unscaledTime - _shownAt < _minShownSeconds)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellation);
            }

            await Tween.Alpha(_group, 0f, _fadeSeconds, useUnscaledTime: true).ToUniTask(cancellation);
            _group.blocksRaycasts = false;
            _root.SetActive(false);
        }

        private void Update()
        {
            if (_root.activeSelf)
            {
                _road.fillAmount = LoadingWave.Fill(Time.unscaledTime - _shownAt, _secondsPerFill);
            }
        }
    }
}
