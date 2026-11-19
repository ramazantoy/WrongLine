using System.Threading;
using AF.Core;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WrongLine.UI
{
    /// <summary>
    /// The scene change cover, on the root prefab so it outlives every scene: it fades in, two mirrored roads fill towards the middle
    /// and draw back until the next scene is ready, and it fades away. All in unscaled time; the game may be paused while it loads.
    /// The roads do not follow the load's progress (they would only be a fake bar); the cover stays up long enough to be seen. The
    /// change that leaves the intro is not covered (<see cref="LoadingCoverRule"/>).
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour, ILoadingScreen
    {
        /// <summary>Seconds the roads take to fill, and as long to empty.</summary>
        private const float WaySeconds = 0.9f;

        /// <summary>The cover stays up at least this long once it is fully shown: a fast load would only flash it.</summary>
        private const float MinShownSeconds = 1.2f;

        [Tooltip("The whole screen. Shown and hidden; this component's object stays active.")]
        [SerializeField, Required] private GameObject _root;

        [SerializeField, Required] private CanvasGroup _group;

        [Tooltip("The road below the text, a vertically filled sprite (fills from its bottom).")]
        [SerializeField, Required] private Image _roadBottom;

        [Tooltip("The same road mirrored above the text.")]
        [SerializeField, Required] private Image _roadTop;

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
            SetRoads(0f);
            _group.alpha = 0f;
            _group.blocksRaycasts = true;
            _root.SetActive(true);
            await Tween.Alpha(_group, 1f, _fadeSeconds, useUnscaledTime: true).ToUniTask(cancellation);
        }

        public void SetProgress(float progress)
        {
            // Not shown: the roads run on their own.
        }

        public async UniTask Hide(CancellationToken cancellation)
        {
            if (!_covering)
            {
                return;
            }

            _covering = false;

            while (Time.unscaledTime - _shownAt < MinShownSeconds)
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
                SetRoads(LoadingWave.Fill(Time.unscaledTime - _shownAt, WaySeconds));
            }
        }

        private void SetRoads(float fill)
        {
            _roadBottom.fillAmount = fill;
            _roadTop.fillAmount = fill;
        }
    }
}
